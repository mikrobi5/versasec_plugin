#include "StdAfx.h"
#include "GlbApp.h"
#include "CmsCoreDll_exports.h"
#include "project_shared/error_str.h"
#include "global/cards/IdPrime840Card.h"
#include "global/cards/IdPrimeCard.h"
#include "global/cards/vsNetCard.h"
#include "global/cards/PivCard.h"
#include "global/cards/MinidriverCard.h"
#include "global/cards/NetPinPolicy.h"
#include "global/helpers/MiniDriverUtils.h"
#include "global/helpers/CapiCertUtil.h"
#include "global/helpers/des_helper.h"
#include "global/helpers/filehelpers.h"
#ifdef _WINDOWS
	#include "global/helpers/SysInfo.h"
#endif
#include "global/helpers/P12Helper.h"
#include "global/helpers/XMLSignature.h"
#include "global/helpers/FileSignature.h"
#include "global/helpers/utils.h"
#include "global/versasec/vsCfgLogApp.h"
#include "keys/vs_mk.h"
#include "shared/misc/CErrorStack.h" 

static TCHAR logTags[]=_T("cmsCoreDll");

//#define _MY_DEBUG

static CMString	_GetProductKey();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class CCmsCoreInst {
public:
	CCmsCoreInst()	{}
};

class CCmsCoreCard {
public:
	IMinidriverCard* m_pCard;
	CCmsCoreCard(IMinidriverCard* pCard)	{ m_pCard=pCard; }
	virtual ~CCmsCoreCard()					{ if(m_pCard) delete m_pCard; }
};

CMCriticalSection						m_cs_Inst;
std::map<HCMSCOREINST, CCmsCoreInst*>	m_Inst;

CMCriticalSection						m_cs_Card;
std::map<ICmsCoreCard, CCmsCoreCard*>	m_Card;

CMCriticalSection						m_cs_P12;
std::map<DWORD, CP12Parser*>			m_P12;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#ifdef _WINDOWS
# include <atlconv.h>				//TODO: Handle global
# undef M_USES_CONVERSION 
# define M_USES_CONVERSION		USES_CONVERSION
//# define W2A(a)  CW2AEX<512>(a)		
#endif

#define BEGIN_TRY(a)	try {	LOG_INDENT_DEBUG_TAG(#a);
#undef END_TRY
#define END_TRY			} catch(...) { SET_OUT_ERROR(CARD_ERR_INTERNAL); return false; }

#undef SET_ERROR
#define SET_ERROR(a)	LOG_ERROR_TAG(_TC("fail %08lx"), a);  if(pRetErr) *pRetErr=a; 

#define CID_ROOT_CERT						9999
#define MAKE_CONTAINER_ID(keySpec,idx)		MAKELPARAM(keySpec,idx);
#define CONTAINER_ID_IDX(a)					HIWORD(a)
#define CONTAINER_ID_KEYSPEC(a)				LOWORD(a)
#define CONTAINER_ID_ISROOTCERT(a)			((CONTAINER_ID_KEYSPEC(a)==CID_ROOT_CERT)?true:false)

CMCriticalSection	g_cs_Rnd;
CMStringArray		g_Rnd;
//CMByteArray		g_arRnd;

#define TAG_DATA		0x01
#define TAG_CREATED		0x02
#define TAG_COUNT		0x03

static CMString	getActivationCodeStorageFileName() {
	CMString	szFileName;
#ifdef _WINDOWS
	DWORD		dwErr;
	int			csidl=CSIDL_LOCAL_APPDATA/*0x001c=28*/;  /*CSIDL_APPDATA 0x001a=26 (roaming)*/
	szFileName = getSpecialFolder(csidl, _T("Gemalto\\scsed"));
	if(!_dir_exists(szFileName) && !CreateFolder(szFileName, &dwErr)) {
		LOG_ERROR_TAG(_TC("failed to create '%s': %s"), szFileName, FormatErrorMessage(dwErr));
	}
	addSlash(szFileName);
	szFileName += _TC("vs_codes.bin");
#else
//#  error JD: TBD
#endif
	return szFileName;
}
static bool storeActivationChallenges() {
	CMSingleLock	_sl(&g_cs_Rnd, true);
	if(!g_Rnd.GetSize()) return true;

	CMByteArray		arData;
	CDataStorage	obj;
	DWORD			dwCnt=(DWORD)g_Rnd.GetSize();
	obj.addBlob(TAG_COUNT, dwCnt);						//Encode
	for(DWORD i=0; i<dwCnt; i++) {
		CMByteArray	arRnd;
		ConvertHexStringToByteArray(g_Rnd[i], arRnd);
		obj.addBlob(TAG_DATA, arRnd);
	}
	obj.compress();										//Compress
	arData.Append(obj.getData());

	CMString	str = _GetProductKey();					//Encrypt with key based on OS-SN
	CMByteArray	arKey;
	Pwd2Key(str, arKey);
	if(!sec_TDESEncrypt(arKey, (LPBYTE)"\x71\x25", 2, 2, arData)) {
		LOG_ERROR_TAG(_TC("Failed to encrypt"));
		ASSERT(0);
		return false;
	}

	CMString	szFileName = getActivationCodeStorageFileName();
	if(!StoreDataInFile(szFileName, arData)) {			//Store
        LOG_ERROR_TAG(_TC("Failed to store data to: %s"), (LPCTSTR)szFileName);
		return false;
	}
	return true;
}

static bool readActivationChallenges() {
	CMSingleLock	_sl(&g_cs_Rnd, true);
	if(g_Rnd.GetSize()) return true;				//Already read
	CMByteArray		arData;
	CMString		szFileName = getActivationCodeStorageFileName();
	if(!GetFileData(szFileName, arData)) {			//Read
        LOG_ERROR_TAG(_TC("Failed to read codes: %s"), (LPCTSTR)szFileName);
		return false;
	}

	CMString	str = _GetProductKey();				//Decrypt with key based on OS-SN
	CMByteArray	arKey;
	Pwd2Key(str, arKey);
	if(!sec_TDESDecrypt(arKey, (LPBYTE)"\x71\x25", 2, 2, arData)) {
        LOG_ERROR_TAG(_TC("Failed to decrypt: %d"), (LPCTSTR)szFileName);
		ASSERT(0);
		return false;
	}

	CDataStorage	obj;
	obj.setData(arData);
	obj.uncompress();								//Uncompress

	DWORD		dwCnt;
	CMByteArray	data;
	if(!obj.getBlobById(TAG_COUNT, dwCnt)) {
        LOG_ERROR_TAG(_TC("Corrupted: %d"), (LPCTSTR)szFileName);
		return false;
	}
	g_Rnd.SetSize(dwCnt);
	for(DWORD i=0; i<dwCnt; i++) {
		CMByteArray	arRnd;
		if(!obj.getBlobById(TAG_DATA, arRnd, i)) continue;
		g_Rnd[i] = ConvertByteArrayToHexString(arRnd);
	}
	LOG_DEBUG_TAG(_TC("%d codes read"), dwCnt);
	return true;
}

EXP_LPCWSTR u16Copy(EXP_LPWSTR dest, std::u16string src)
{
   // std::u16string buffer(src);
    //std::wcout << L"cp src: " << std::wstring(src.begin(), src.end()) << std::endl;
    unsigned i;
    for(i=0; src[i] != '\0'; ++i)
    {
        dest[i] = src.c_str()[i];
    }
    dest[i] = '\0';
    return dest;
}

std::u16string String2Unicode(LPCTSTR pszStrData)
{
	std::string ansiBuffer(pszStrData);
  //  std::cout << "s2u a: " << ansiBuffer << std::endl;
    std::u16string buffer(ansiBuffer.begin(),ansiBuffer.end());
   // std::basic_string<char16_t> buffer(ansiBuffer.begin(),ansiBuffer.end());
  // std::cout << "s2u r: " << pszStrData << std::endl;
   // std::wcout << L"s2u u: " << buffer.size() << " " << std::wstring(buffer.begin(), buffer.end()) << std::endl;
    return buffer;
}

std::string Unicode2String(EXP_LPCWSTR pszStrData)
{
#ifndef __GNUC__		
	return CW2CT(pszStrData);
#else
	std::basic_string<char16_t> buffer(pszStrData);
    std::string ansiBuffer(buffer.begin(), buffer.end());

    return ansiBuffer;
#endif
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static bool	handleOutString(EXP_LPWSTR* pStrOut, std::u16string pszStrData, fkt_alloc p) {
	if (!pStrOut) return false;
    *pStrOut = (EXP_LPWSTR)p((pszStrData.length() + 1) * sizeof(EXP_WCHAR));
#ifndef __GNUC__		
//	wcscpy(*pStrOut, pszStrData.c_str());
#else
    std::cout << L"#### ho src: " << pszStrData.data() << std::endl;
	u16Copy(*pStrOut, pszStrData);
	std::u16string buffer(*pStrOut);
	std::wcout << L"ho u: " << std::wstring(buffer.begin(), buffer.end()) << std::endl;
#endif
	return true;
}

static bool	handleOutString(EXP_LPWSTR* pStrOut, LPCTSTR pszStrData, fkt_alloc p) {
	if(!pStrOut) return false;
	M_USES_CONVERSION;
#ifndef __GNUC__		
	*pStrOut = (EXP_LPWSTR)p((_tcslen(pszStrData) + 1) * sizeof(EXP_WCHAR));
	wcscpy(*pStrOut, LPCTSTR2WChar(pszStrData));
	return true;
//	return handleOutString(pStrOut, LPCTSTR2WChar(pszStrData), p);
#else
	return handleOutString(pStrOut, String2Unicode(pszStrData), p);
#endif
}

static bool	handleOutArray(LPBYTE* pOutBuf, DWORD* pOutBufSize, CMByteArray& arData, fkt_alloc p) {
	if(!pOutBuf || !pOutBufSize) return false;
    *pOutBuf = (LPBYTE)p(arData.GetSize());
	if(*pOutBuf) {
		*pOutBufSize = (DWORD)arData.GetSize();
		memcpy(*pOutBuf, &arData[0], arData.GetSize());
		return true;
	}
	return false;
}

static void	handleOutError(IErrorStack* pErr, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	if(pErr == nullptr) return;
	CMString	szXml = pErr->getAsXml();
	handleOutString(pRetErr, szXml, p);
}

static void	setOutError(DWORD dwErr, CMSCORE_ERR* pRetErr, fkt_alloc p, LPCTSTR pszFile, LPCTSTR pszFunction, int iLine) {
	CErrorStackNew	oErr;
	oErr.add(dwErr, "", pszFile, pszFile, iLine, CErrorStackComponent_Module());
	handleOutError(&oErr, pRetErr, p);
}
#define SET_OUT_ERROR(a)	setOutError(a, pRetErr, p, __FILE__, __FUNCTION__, __LINE__)

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#include <cardstatusnotifymngmt.h>
CardStatusNotifyMngmt::CardStatusNotifyMngmt()
{

}

void CardStatusNotifyMngmt::Add(ICmsCoreCardStatusChangeNotify*p){
    CMSingleLock	_sl(&m_cs_Notify, true);
    auto it = std::find(m_pNotifies.begin(), m_pNotifies.end(), p);
    if(it == m_pNotifies.end()) { m_pNotifies.push_back(p) ;}
}

void CardStatusNotifyMngmt::Delete(ICmsCoreCardStatusChangeNotify *p){
    CMSingleLock	_sl(&m_cs_Notify, true);
    auto it = std::find(m_pNotifies.begin(), m_pNotifies.end(), p);
    if (it != m_pNotifies.end()) { m_pNotifies.erase(it); }
}

void CardStatusNotifyMngmt::OnCardInsert(IBasicPCSC *pPcsc){
    CMSingleLock	_sl(&m_cs_Notify, true);
    std::for_each(m_pNotifies.begin(), m_pNotifies.end(), [&](ICmsCoreCardStatusChangeNotify* p){
       p->OnCardInsert((ICmsCoreBasicPcsc *) pPcsc);
    });
}

void CardStatusNotifyMngmt::OnCardRemove(IBasicPCSC *pPcsc){
    CMSingleLock	_sl(&m_cs_Notify, true);
    std::for_each(m_pNotifies.begin(), m_pNotifies.end(), [&](ICmsCoreCardStatusChangeNotify* p){
       p->OnCardRemove((ICmsCoreBasicPcsc *) pPcsc);
    });
}

void CardStatusNotifyMngmt::Register(){
    if(!m_pNotifyObjCnt)
        GetCardList()->AddCardStatusNotifier(this);

    m_pNotifyObjCnt++;
}

void CardStatusNotifyMngmt::Unregister(){
    m_pNotifyObjCnt--;
    if(!m_pNotifyObjCnt)
        GetCardList()->DelCardStatusNotifier(this);
}

static CardStatusNotifyMngmt b;

CmsCore_API bool	CmsCore_Initialize(HCMSCOREINST* pOutInst, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_Initialize)
	if(!pOutInst) {
		SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
		return false;
	}
	CMSingleLock	_sl(&m_cs_Inst, true);
	*pOutInst = ::GetTickCount();
	m_Inst[*pOutInst] = new CCmsCoreInst();

/*
	{
		DWORD                   dwErr=0;
		CAutoPtr<IKeyContainer> oKeyContainer;
		CMiniDriverUtils        o;
		o.getKeyContainer("d:\\vm\\shared\\5\\1000\\Alice.p12", "123456", oKeyContainer, true, &dwErr);
		return true;
	}
*/
#ifndef _MY_DEBUG
    initGlbAppObject();	//To make sure it is initialized!!!
    b.Register();
#endif
	readActivationChallenges();

	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_Finalize(HCMSCOREINST hInst) 
{	BEGIN_TRY(CmsCore_Finalize)
	CMSingleLock	_sl(&m_cs_Inst, true);
	if(m_Inst.find(hInst) == m_Inst.end())
		return false;
	delete m_Inst[hInst];
	m_Inst.erase(hInst);

    b.Unregister();
	freeGlbApp();

	return true;
	} catch(...) { return false; }
}

CmsCore_API bool	CmsCore_GetErrorDetails(DWORD err, EXP_LPWSTR* pOutErrStr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_GetErrorDetails)
	CMString	str = getErrorString(err, NULL);
    handleOutString(pOutErrStr, str.GetBuffer(), p);
   // std::cout << "bfr: " << str.GetBuffer() << " mfc: " << str << std::endl;
   // std::wcout << L" utf: " << **pOutErrStr << std::endl;
	return true;
	} catch(...) { return false; }
}

static CMString	_GetProductKey()
{
	CMString strResult;        //Return a Window Product Key
#ifdef _WINDOWS

	HKEY hRegistryKey;        //Registry Handler 
	BYTE   *DigitalProductID=NULL; //Digital Product Key Value 
	DWORD DataLength;         //Digital Product Key Length 

	// HKLM\\SOFTWARE\\MICROSOFT\\Windows NT\\CurrentVersion &#50676;&#44592;  
	if(RegOpenKeyEx(HKEY_LOCAL_MACHINE, _TC("SOFTWARE\\MICROSOFT\\Windows NT\\CurrentVersion"), 
		REG_OPTION_NON_VOLATILE, KEY_READ|KEY_WOW64_64KEY, 
		&hRegistryKey) == ERROR_SUCCESS)
	{

		DataLength = 164; 

		//Allocate Memory
		DigitalProductID = (BYTE *)malloc(DataLength);    

		//Memory Initializationd
		memset(DigitalProductID, 0, DataLength); 

		//Digital Product Key Open

		if(RegQueryValueEx(hRegistryKey, "DigitalProductId", 
			NULL, NULL, DigitalProductID, &DataLength) == ERROR_SUCCESS)
		{   
			strResult= ConvertByteArrayToHexString(DigitalProductID, DataLength);	//JD: Quick hack, as the conversion in SysInfo was not working 
		}
		else
		{
			ASSERT(0);
		}
	}
	else 
	{
		ASSERT(0);
		return strResult;
	}

	//Close Registry
	RegCloseKey(hRegistryKey); 

	//Release Memory
	if(DigitalProductID) free(DigitalProductID); 
#else
//#  error JD: TBD
#endif
	return strResult;
}

static CMString	calcLicenseCode()
{
	CMString	str = _GetProductKey();
	CMByteArray	arKey;
	Pwd2Key(str, arKey);

	CMByteArray	arVal;
	getRandom(arVal, 16);
	memcpy(&arVal[0], "Versatile Security Sweden AB", 16);

	
//	if(!sec_TDESEncrypt(arKey, magic, arVal)) {
	if(!simple_TDESEncrypt(arVal, arKey, arVal)) {
		ASSERT(0);
		return _TC("");
	}
	return ConvertByteArrayToHexString(arVal);
}

/************************************************************************************************************************************************/
#define VSCFG_CARDACCESS						CFG_SOURCE_TYPE_DWORD	\
												VSCFG_SETTING_STD_R(VSCFG_CARDACCESS,_T("/card/support_md")) CFG_SOURCE_DEFAULT(_T("1"))
DWORD	g_dwCardAccess=0;
#define MD_DISABLED		1
#define MD_ENABLED		2

#define VSCFG_BY_PARAM(pszParam)				FindAndReplace(CFG_SOURCE_TYPE_STRING	\
												VSCFG_SETTING_STD_RW(VSCFG_BY_PARAM,_T("#p#")) CFG_SOURCE_DEFAULT(_T("")), \
												_T("#p#"), pszParam)

CmsCore_API bool	CmsCore_GetConfigParam					(EXP_LPCWSTR pszCfgFile, EXP_LPCWSTR pszKey, EXP_LPWSTR* pOutValue, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_GetConfigParam)

	M_USES_CONVERSION;
    
#ifndef __GNUC__
    CMString	str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(WChar2LPCTSTR(pszKey)));
#else
    CMString	str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(Unicode2String(pszKey).c_str()));
#endif
	handleOutString(pOutValue, str, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_GetLicenseParam					(EXP_LPCWSTR pszLicFile, EXP_LPCWSTR pszPubKey, EXP_LPCWSTR pszKey, EXP_LPWSTR* pOutValue, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_GetLicenseParam)
#ifndef __GNUC__
            auto length = !wcslen(pszLicFile);
        #else
            auto length = !std::char_traits<char16_t>::length(pszLicFile);
        #endif
    if(!pszLicFile || length) {	//Look at OEM signed license section in CFG
		CMString	str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(_TC("/license/code")));
		CMString	strCode = calcLicenseCode();
		if(str != strCode) {
            LOG_ERROR_TAG(_TC("invalid code: %s-%s"), (LPCTSTR)str, (LPCTSTR)strCode);
			SET_OUT_ERROR(CARD_ERR_TYPE_DOES_NOT_MATCH);
			return false;
		}

		M_USES_CONVERSION;
#ifndef __GNUC__
    str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(WChar2LPCTSTR(pszKey)));
#else
    std::basic_string<char16_t> u16Buffer(pszKey);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(buffer.c_str()));
#endif
		handleOutString(pOutValue, str, p);
		return true;
	}
	M_USES_CONVERSION;
	CMString	szPubKey;
#ifndef __GNUC__
	if(pszPubKey && wcslen(pszPubKey)) {
#else
    if(pszPubKey && std::char_traits<char16_t>::length(pszPubKey)) {
#endif
        szPubKey = Unicode2String(pszPubKey).c_str();
	} else {
		szPubKey =
#include "global/versasec/testkey_pub.inc"
	for (int i = 0; i < szPubKey.GetLength(); i++) szPubKey.SetAt(i, (char)(255 - (BYTE)(szPubKey[i])));
	}

	CSignedXmlConfigProvider* SigFile = new CSignedXmlConfigProvider();
#ifndef __GNUC__
    if (SigFile->LoadXmlFile(WChar2LPCTSTR(pszLicFile), _T("/license"), false)) {
#else
    std::basic_string<char16_t> u16Buffer3(pszLicFile);
    std::string buffer3(u16Buffer3.begin(), u16Buffer3.end());
    if (SigFile->LoadXmlFile(buffer3.c_str(), _T("/license"), false)) {
#endif
		if(SigFile->VerifySignature(szPubKey)) {
			CConfig		m_cfg;
			m_cfg.AddProvider(VSCFG_NAME_TEMP, SigFile, false);

#ifndef LINUX
      CMString	str = m_cfg.GetSTRING(VSCFG_BY_PARAM(WChar2LPCTSTR(pszKey)));
#else
    std::basic_string<char16_t> u16Buffer4(pszKey);
    std::string buffer4(u16Buffer4.begin(), u16Buffer4.end());
     CMString	str = m_cfg.GetSTRING(VSCFG_BY_PARAM(buffer4.c_str()));
#endif
			handleOutString(pOutValue, str, p);
			m_cfg.Clean();
			return true;
		} else {
			SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
			delete SigFile;
		}
	} else {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		delete SigFile;
	}
	return false;
	END_TRY
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Readers/Cards/List
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_CardList_Get					(HCMSCOREINST hInst, ICmsCoreCardList* pOutCardList, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_Get)
	CMSingleLock	_sl(&m_cs_Inst, true);
	if(m_Inst.find(hInst) == m_Inst.end()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
    CGlbAppObject* pApp = GetGlbAppObj();
   // auto test = GetCardList();
	if(!GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_CONNECTION_FAILED);
		return false;
	}

	if(pOutCardList) {
		*pOutCardList = (ICmsCoreCardList*)GetCardList();
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_UpdateReaderList		(ICmsCoreCardList hCardList, ICmsCoreReaderList* pReaderList, bool bCardPrinterOnly, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_UpdateReaderList)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	if(!pReaderList) {
		SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
		return false;
	}

	M_USES_CONVERSION;
	ICardList* pCardList = GetCardList();
	bool bRet=false;
	try {
		pReaderList->bReaderSelected=false;
		if(pCardList) {
			int	iRdCnt = pCardList->GetAvailableCardsCnt(GetOperatorCardReaderName());
			if(bCardPrinterOnly) iRdCnt=0;
			int iPrCnt = 0;
			int	iCrdCnt = 0;
			DWORD dwLastUpdateTS = pCardList->GetLastStatusChangeTimestamp();
			if(getCardPrinter()) {
				if(getCardPrinter()->isAvailable(true)) {
					iPrCnt = 1;
					if(getCardPrinter()->GetLastStatusChangeTimestamp() > dwLastUpdateTS)
						dwLastUpdateTS = (DWORD)getCardPrinter()->GetLastStatusChangeTimestamp();
				}
			}
			if(!(iRdCnt+iPrCnt)) {
				pReaderList->ResetContent();
				if(pReaderList->bLastReaderAvail) 
					bRet=true;
				pReaderList->bLastReaderAvail = false;
			} else if(	dwLastUpdateTS != pReaderList->dwLastUpdate /*iRdCnt != lastReaderCnt*/) {
				int cnt=0, oldidx=CB_ERR;
				//		CMString selReader = AfxGetApp()->GetProfileString(_TC("settings"), _TC("reader"), _TC(""));
				CMString oldReader, szPrinterReader;
				oldidx = pReaderList->GetCurSel();
				if(oldidx != CB_ERR)
                {
#ifndef __GNUC__
					oldReader = pReaderList->get(oldidx);
#else
                    //std::basic_string<char16_t> readerName(pReaderList->get(oldidx));
                    //std::string test(readerName.begin(), readerName.end());
                    oldReader.Append(Unicode2String(pReaderList->get(oldidx)).c_str());
#endif
                }
				
					
				pReaderList->ResetContent();
//				if(oldReader.IsEmpty()) oldReader = m_szLastSelectedReader;
#ifndef __GNUC__
                if(wcslen(pReaderList->szForceSelectedReader))
#else
                if(std::char_traits<char16_t>::length(pReaderList->szForceSelectedReader))
#endif
                    oldReader = Unicode2String(pReaderList->szForceSelectedReader).c_str();

				for (int i = 0; i < iRdCnt; i++) {
					CMString p = pCardList->GetCardReaderName(i, GetOperatorCardReaderName());
					if(getCardPrinter() && getCardPrinter()->isAvailable(true)) {
						if(getCardPrinter()->getReaderName() == p) 
							continue;
					}

					if(p.GetLength()) {
#ifndef __GNUC__
                        pReaderList->add(LPCTSTR2WChar(p));
#else
                        std::string buffer(p);
                        std::basic_string<char16_t> test(buffer.begin(), buffer.end());
                        pReaderList->add(test.c_str());
#endif
						cnt++;
						iCrdCnt++;
					}
				}
				if(iPrCnt && getCardPrinter()) {
#ifndef __GNUC__
                    pReaderList->add(LPCTSTR2WChar(getCardPrinter()->getName()));
#else
                    pReaderList->add(String2Unicode(getCardPrinter()->getName()).c_str());
#endif
					szPrinterReader = getCardPrinter()->getReaderName();
					cnt++;
				}
				int idx = CB_ERR;
				if(pCardList->isCardAvailable(oldReader) || pCardList->isCardAvailable(szPrinterReader)) {	//look only if card is available
#ifndef __GNUC__
                    idx = pReaderList->find(LPCTSTR2WChar(oldReader));
#else
             		std::string buffer(oldReader);
                    std::basic_string<char16_t> uchar16Buffer(buffer.begin(), buffer.end());
                    idx = pReaderList->find(uchar16Buffer.c_str());
#endif
				} else {
					if(!iCrdCnt) {	//Still keep old one, because we do not have any cards avail
#ifndef __GNUC__
                        idx = pReaderList->find(LPCTSTR2WChar(oldReader));
#else
						std::string buffer(oldReader);
                        std::basic_string<char16_t> uchar16Buffer(buffer.begin(), buffer.end());
                        idx = pReaderList->find(uchar16Buffer.c_str());
#endif
					}
				}
				if((idx == CB_ERR) && cnt) bRet=true;
//			idx = PCSCReader.FindStringExact(-1, selReader);
				pReaderList->SetCurSel(idx == CB_ERR ? 0 : idx);

				if(cnt) { 
					pReaderList->bReaderSelected=true;
				}
				if(!pReaderList->bLastReaderAvail) 
					bRet=true;
				pReaderList->bLastReaderAvail = true;
				pReaderList->dwLastUpdate = dwLastUpdateTS;
			} else if(pReaderList->lastReaderCnt) {
				pReaderList->bReaderSelected=true;
			}
			pReaderList->lastReaderCnt = iRdCnt+iPrCnt;
		}

		int idx = pReaderList->GetCurSel();
		if(idx != LB_ERR) {
#ifndef __GNUC__
			IBasicPCSC* pPcsc = pCardList->GetCardPcsc(CMString(pReaderList->get(idx)));
#else			
	        std::basic_string<char16_t> buffer(pReaderList->get(idx));
            std::string test(buffer.begin(),buffer.end());
            IBasicPCSC* pPcsc = pCardList->GetCardPcsc(test.c_str());
#endif		
			if(pPcsc) {
				if(!bRet) {	//no changes so far
					if(pPcsc->getLastWrite() > pReaderList->dwLastReaderWrite) { //check if we need to return a CHANGE because there was a former WRITE to this reader (to update dialog contents
						bRet=true;
						pReaderList->dwLastReaderWrite = pPcsc->getLastWrite();
					}
				} else {
					pReaderList->dwLastReaderWrite = pPcsc->getLastWrite();
				}
			}
		}
	} catch(...) { }
    return true;
//	return bRet;

	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_GetAvailableCardsCnt	(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderToSkip, UINT* outCnt, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_GetAvailableCardsCnt)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}

	M_USES_CONVERSION;
	if(outCnt)
	{
#ifndef LINUX
        *outCnt = GetCardList()->GetAvailableCardsCnt(CW2T(pszReaderToSkip));
#else
        std::basic_string<char16_t> u16Buffer(pszReaderToSkip);
        std::string buffer(u16Buffer.begin(), u16Buffer.end());
        *outCnt = GetCardList()->GetAvailableCardsCnt(buffer.c_str());
#endif
    }
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_GetCardPcsc			(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderName, ICmsCoreBasicPcsc* pOuthPcsc, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_GetCardPcsc)

#ifdef _MY_DEBUG
	if(!hCardList) {	//Create (JD: Just for DEBUGGING
		CBasicPCSC	*p = new CBasicPCSC();
		if(!p) return false;

		DWORD		status;
		CMArray<BYTE, BYTE> atr;

		M_USES_CONVERSION;
        p->SetReaderName(WChar2LPCTSTR(pszReaderName));
		if(p->OpenContext(SCARD_SCOPE_USER) == SCARD_S_SUCCESS) {
			if(p->GetStatus( status, atr, false, NULL, 1) ) 
			{
				if( (status&SCARD_STATE_PRESENT) && atr.GetSize()) {
					if(pOuthPcsc)
						*pOuthPcsc = p;
					return true;
				}
			}
		}
	}
#endif //_MY_DEBUG
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}

	ASSERT(GetCardList());

	M_USES_CONVERSION;
	if(pOuthPcsc)
	{
#ifndef LINUX
		*pOuthPcsc = GetCardList()->GetCardPcsc(CW2T(pszReaderName));
#else
        std::basic_string<char16_t> u16Buffer(pszReaderName);
        std::string buffer(u16Buffer.begin(), u16Buffer.end());
        *pOuthPcsc = GetCardList()->GetCardPcsc(buffer.c_str());
#endif
    }
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_clearCaches			(ICmsCoreCardList hCardList, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_clearCaches)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}

	GetCardList()->clearCaches();
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_GetCardReaderName		(ICmsCoreCardList hCardList, UINT Idx, EXP_LPCWSTR pszReaderToSkip, EXP_LPWSTR* pOutReaderName, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CardList_GetCardReaderName)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}

	M_USES_CONVERSION;
#ifndef LINUX
	CMString	szReaderName = GetCardList()->GetCardReaderName(Idx, CW2T(pszReaderToSkip));
#else
        std::basic_string<char16_t> u16Buffer(pszReaderToSkip);
        std::string buffer(u16Buffer.begin(), u16Buffer.end());
        CMString	szReaderName = GetCardList()->GetCardReaderName(Idx, buffer.c_str());
#endif
	if(szReaderName.IsEmpty()) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	handleOutString(pOutReaderName, szReaderName, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_isCardAvailable		(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderName, bool* pOutAvail, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_isCardAvailable)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}

	M_USES_CONVERSION;
	if(pOutAvail)
	{
#ifndef LINUX
        *pOutAvail = GetCardList()->isCardAvailable(CW2T(pszReaderName));
#else
        std::basic_string<char16_t> u16Buffer(pszReaderName);
        std::string buffer(u16Buffer.begin(), u16Buffer.end());
        *pOutAvail = GetCardList()->isCardAvailable(buffer.c_str());
#endif
    }
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_isValid				(ICmsCoreCardList hCardList, ICmsCoreBasicPcsc hPcsc, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_isValid)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	return GetCardList()->isValid((IBasicPCSC *)hPcsc);
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_AddCardStatusNotifier	(ICmsCoreCardList hCardList, ICmsCoreCardStatusChangeNotify*pNotify, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_AddCardStatusNotifier)
	if(hCardList != GetCardList()) {
        GetCardList()->AddCardStatusNotifier(&b);
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
    b.Add(pNotify);	
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CardList_DelCardStatusNotifier	(ICmsCoreCardList hCardList, ICmsCoreCardStatusChangeNotify*pNotify, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CardList_DelCardStatusNotifier)
	if(hCardList != GetCardList()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
    b.Delete(pNotify);
	SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
	return true;
	END_TRY
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//PCSC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Cards General
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_CardSupported					(HCMSCOREINST hInst, LPBYTE atr, size_t atrSize, DWORD* pOutCardType, EXP_LPWSTR* pOutCardName, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CardSupported)
	CMSingleLock	_sl(&m_cs_Inst, true);
	if(m_Inst.find(hInst) == m_Inst.end()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	CMByteArray	arAtr;
	setArrayData(arAtr, atr, atrSize);
	{
		CIdPrimeCard	oCard;
		if(oCard.supported(arAtr)) {
			if(pOutCardType) *pOutCardType=1;
			handleOutString(pOutCardName, oCard.getName(), p);
			return true;
		}
	}

	{
		CNetCard	oCard;
		if(oCard.supported(arAtr)) {
			if(pOutCardType) *pOutCardType=1;
			handleOutString(pOutCardName, oCard.getName(), p);
			return true;
		}
	}

	{
		CMinidriverCard	oCard(getMdCfg());
		if(oCard.supported(arAtr)) {
			if(pOutCardType) *pOutCardType=1;
			handleOutString(pOutCardName, oCard.getName(), p);
			return true;
		}
	}

// 	{
// 		CIdPrime840Card	oCard;
// 		if(oCard.supported(atr, atrSize)) {
// 			if(pOutCardType) *pOutCardType=1;
// 			handleOutString(pOutCardName, oCard.getName(), p);
// 			return true;
// 		}
// 	}

	return false;
	END_TRY
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//PCSC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_Pcsc_GetAtr						(ICmsCoreBasicPcsc hPcsc, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_Pcsc_GetAtr)
	if(!GetCardList()->isValid((IBasicPCSC *)hPcsc)) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	CMByteArray		arAtr;
	((IBasicPCSC *)hPcsc)->getAtr(arAtr);

	handleOutArray(pOutData, pOutDataSize, arAtr, p);
	return true;
	END_TRY
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Card Object
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_Create_CmsCard					(ICmsCoreBasicPcsc hPcsc, ICmsCoreCard* pOuthCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_Create_CmsCard)
#ifndef _MY_DEBUG
	if(!GetCardList()->isValid((IBasicPCSC *)hPcsc)) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
#endif

	CMByteArray		arAtr;
	((IBasicPCSC *)hPcsc)->getAtr(arAtr);

    {
        CNetCard	oCard;
        if(oCard.supported(arAtr)) {
            if(pOuthCard) {
                CMSingleLock	_sl(&m_cs_Card, true);
                *pOuthCard = (ICmsCoreCard)::GetTickCount();
                m_Card[*pOuthCard] = new CCmsCoreCard(new CNetCard());
                m_Card[*pOuthCard]->m_pCard->Attach((IBasicPCSC *)hPcsc);
            }
            return true;
        }
    }

#ifndef NO_PIV_SUPPORT
	{
		CPivCard*	pCard = CPivCard::createPivCard(arAtr, getMdCfg());
		if (pCard != nullptr) {
			if (pOuthCard) {
				CMSingleLock	_sl(&m_cs_Card, true);
				*pOuthCard = (ICmsCoreCard)::GetTickCount();
				m_Card[*pOuthCard] = new CCmsCoreCard(pCard);
				m_Card[*pOuthCard]->m_pCard->Attach((IBasicPCSC *)hPcsc);
			}

            return true;
		}
    }
#endif

	if(g_dwCardAccess) {
		g_dwCardAccess = GetGlobalConfig().GetDWORD(VSCFG_CARDACCESS);
	}

	
	if(g_dwCardAccess == MD_ENABLED) 
	{
		CMinidriverCard	oCard(getMdCfg());
		if(oCard.supported(arAtr)) {
			if(pOuthCard) {
				CMSingleLock	_sl(&m_cs_Card, true);
				*pOuthCard = (ICmsCoreCard)::GetTickCount();
				m_Card[*pOuthCard] = new CCmsCoreCard(new CMinidriverCard(getMdCfg()));
				m_Card[*pOuthCard]->m_pCard->Attach((IBasicPCSC *)hPcsc);
			}
			return true;
		}
	}

	{
		CIdPrimeCard	oCard;
		if(oCard.supported(arAtr)) {
			if(pOuthCard) {
				CMSingleLock	_sl(&m_cs_Card, true);
				*pOuthCard = (ICmsCoreCard)::GetTickCount();
				m_Card[*pOuthCard] = new CCmsCoreCard(new CIdPrimeCard());
				m_Card[*pOuthCard]->m_pCard->Attach((IBasicPCSC *)hPcsc);
			}
			return true;
		}
	}

	SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
	return false;
	END_TRY
}

/************************************************************************************************************************************************/
static bool initCard(ICmsCoreCard hCard, IMinidriverCard*& outCard, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	CMSingleLock	_sl(&m_cs_Card, true);
	if(m_Card.find(hCard) == m_Card.end()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	if(!m_Card[hCard]) {
		SET_OUT_ERROR(CARD_ERR_INTERNAL);
		return false;
	}
	outCard = m_Card[hCard]->m_pCard;
	return true;
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_Delete_CmsCard					(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_Delete_CmsCard)
	CMSingleLock	_sl(&m_cs_Card, true);
	if(m_Card.find(hCard) == m_Card.end()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	if(m_Card[hCard]) {
//		m_Card[hCard]->m_pCard->IChallengeResponseCard::DisConnect();	//JD: We do SCARD_RESET_CARD here, because we saw issues in India where later SCardCnnect did return SCARD_RESET_CARD
		delete m_Card[hCard];
	}
	m_Card.erase(hCard);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_identifyCard			(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_identifyCard)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	if(!pCard->IdentifyCard()) {
		SET_OUT_ERROR(CARD_ERR_CONNECTION_FAILED);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_lockCard				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_lockCard)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	//TBD
	SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
	return false;
	END_TRY
}
CmsCore_API bool	CmsCore_CmsCard_unlockCard				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_unlockCard)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	//TBD
	SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
	return false;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_emptyCache				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_emptyCache)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	pCard->GetBasicPcsc()->getCache().clear();
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_get_CSN_CARDID			(ICmsCoreCard hCard, EXP_LPWSTR* pOutCSN, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_get_CSN_CARDID)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMString csn = pCard->get_CSN_CARDID();
	if(csn.IsEmpty()) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	handleOutString(pOutCSN, csn, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_get_CSN_CSN				(ICmsCoreCard hCard, EXP_LPWSTR* pOutCSN, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_get_CSN_CSN)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMString csn = pCard->GetCSN();
	if(csn.IsEmpty()) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	handleOutString(pOutCSN, csn, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getChallenge			(ICmsCoreCard hCard, LPBYTE* pOutCryptogram, DWORD* pOutCryptogramSize, fkt_alloc p, CMSCORE_ERR* pRetErr) 
{	BEGIN_TRY(CmsCore_CmsCard_getChallenge)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMByteArray		arData;
	long	lErr = pCard->GetChallenge(arData);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	handleOutArray(pOutCryptogram, pOutCryptogramSize, arData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_unblockUserPin			(ICmsCoreCard hCard, LPBYTE cryptogram, size_t cryptogramSize, EXP_LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_unblockUserPin)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
	#ifndef LINUX
	long	lErr = pCard->UnblockUserPin( cryptogram, (UINT)cryptogramSize, W2A(newPin), iTriesLeft);
#else
    std::basic_string<char16_t> u16Buffer(newPin);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    long	lErr = pCard->UnblockUserPin( cryptogram, (UINT)cryptogramSize, buffer.c_str(), iTriesLeft);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin			(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR puc, EXP_LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p)
{
    BEGIN_TRY(CmsCore_CmsCard_unblockRolePin)
    IMinidriverCard*	pCard;
    if(!initCard(hCard, pCard, pRetErr, p)) return false;

    LOG_DEBUG_TAG(_TC("role=%d"), bRole);

    M_USES_CONVERSION;
#ifndef LINUX
    long	lErr = pCard->UnblockRolePin( bRole, PIV_ROLE_PUC, W2A(puc), (LPBYTE)W2A(newPin), newPin?wcslen(newPin):0, iTriesLeft);
#else
    std::basic_string<char16_t> u16Buffer(puc);
    std::basic_string<char16_t> u16Buffer2(newPin);
    std::string pucBuffer(u16Buffer.begin(), u16Buffer.end());
    std::string newPinBuffer(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->UnblockRolePin(bRole, PIV_ROLE_PUC,pucBuffer.c_str(), (LPBYTE)newPinBuffer.c_str(), (UINT)newPinBuffer.size(), iTriesLeft);
#endif
    if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
        return false;
    }
    return true;
    END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin1(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE cryptogram, size_t cryptogramSize, LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	CErrorStackNew	oErr;
	{
		IErrorStack* pRetErr = &oErr;
		ADD_ERROR_WIN2(ERROR_CALL_NOT_IMPLEMENTED, _TC("Test message"));
	}
	handleOutError(&oErr, pRetErr, p);
	return false;
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin2(ICmsCoreCard hCard, PIN_ID bRole, PIN_ID bPukRole, LPCWSTR pszPuk, LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	CErrorStackNew	oErr;
	{
		IErrorStack* pRetErr = &oErr;
		ADD_ERROR_WIN2(ERROR_CALL_NOT_IMPLEMENTED, _TC("Test message"));
	}
	handleOutError(&oErr, pRetErr, p);
	return false;
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getRoleTriesLeft		(ICmsCoreCard hCard, PIN_ID bRole, DWORD* pOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_getRoleTriesLeft)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	DWORD	dwTriesLeft=0;
	long	lErr = pCard->GetRoleTriesLeft(bRole, dwTriesLeft);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(pOutCnt) *pOutCnt = dwTriesLeft;
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getAdminTriesLeft		(ICmsCoreCard hCard, DWORD* pOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_getAdminTriesLeft)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	DWORD	dwTriesLeft=0;
	long	lErr = pCard->GetAdminTriesLeft(dwTriesLeft);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(pOutCnt) *pOutCnt = dwTriesLeft;
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_changeRolePin			(ICmsCoreCard hCard,  PIN_ID bRole, EXP_LPCWSTR oldPin, EXP_LPCWSTR newPin, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	BEGIN_TRY(CmsCore_CmsCard_changeRolePin)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	M_USES_CONVERSION;
	#ifndef LINUX
	long	lErr = pCard->ChangeRolePin( bRole, W2A(oldPin), W2A(newPin));
#else
    std::basic_string<char16_t> u16Buffer(oldPin);
    std::basic_string<char16_t> u16Buffer2(newPin);
    std::string oldPinBuffer(u16Buffer.begin(), u16Buffer.end());
    std::string newPinBuffer(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->ChangeRolePin( bRole,oldPinBuffer.c_str(), newPinBuffer.c_str() );
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_loginRole				(ICmsCoreCard hCard,  PIN_ID bRole, EXP_LPCWSTR pin, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_loginRole)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	M_USES_CONVERSION;
	#ifndef LINUX
	long	lErr = pCard->VerifyRolePin(bRole, W2A(pin));
#else
    std::basic_string<char16_t> u16Buffer2(pin);
    std::string pinBuffer(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->VerifyRolePin( bRole, pinBuffer.c_str());
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_roleAuthenticated		(ICmsCoreCard hCard, PIN_ID bRole, bool* pOutAuthenticated, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_roleAuthenticated)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	M_USES_CONVERSION;
	bool	bIsAuthenticated=false;
	long	lErr = pCard->isRoleAuthenticated(bRole, bIsAuthenticated);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	LOG_DEBUG_TAG(_TC("auth=%s"), LOG_BOOL(bIsAuthenticated));
	if(pOutAuthenticated) *pOutAuthenticated = bIsAuthenticated;
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_loginAdmin				(ICmsCoreCard hCard,  LPBYTE cryptogram, size_t cryptogramSize, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_loginAdmin)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMByteArray	arData;
	setArrayData(arData, cryptogram, cryptogramSize);
	long	lErr = pCard->LoginAdmin(arData, NULL);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
void LogKeyToDatFile(CMByteArray& key, LPCTSTR csn, long lErr)
{
#ifdef VS_DEBUG1
	CMString	s1 = ConvertByteArrayToHexString(key);
	FILE *fp = fopen("c:\\3\\net_user_keys.dat", "a");
	if(fp) { 
		M_USES_CONVERSION;
		fprintf(fp, "%s: %s  res=%08lx OC_CSN: %s\n", LPCTSTR2ConstChar(csn), LPCTSTR2ConstChar(s1), lErr, LPCTSTR2ConstChar(GetOperatorCard()?GetOperatorCard()->GetCSN():_TC(""))); 
		fclose(fp); 
	}
#endif
}

CmsCore_API bool	CmsCore_CmsCard_setAdminKey				(ICmsCoreCard hCard,  LPBYTE cryptogram, size_t cryptogramSize, LPBYTE newKey, size_t newKeySize, int iNewTries, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_setAdminKey)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMByteArray	arNewKey;
	setArrayData(arNewKey, newKey, newKeySize);
	long	lErr = pCard->SetAdminKey( cryptogram, (UINT)cryptogramSize, arNewKey, iNewTries);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
#ifdef VS_DEBUG
	CMByteArray	arData;
	setArrayData(arData, newKey, newKeySize);
	LogKeyToDatFile(arData, pCard->GetCSN(), lErr);
#endif
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_blockRolePin			(ICmsCoreCard hCard,  PIN_ID bRole, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_blockRolePin)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CAutoPtr<IPinPolicy>	pPinPolicy(pCard->readPinPolicy(bRole));
	CMString				validPin = pPinPolicy->generateValidPin(pCard);
// 	if(!pCard->canPinBeBlocked(bRole)) {
// 		LOG_DEBUG_TAG(_TC(" pin %d skipped: not supported by card"), bRole);
// 		SET_OUT_ERROR(CARD_ERR_NOT_SUPPORTED);
// 		return false;
// 	}

	long	lErr = pCard->BlockRolePin( bRole, validPin);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_readFile				(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_readFile)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

    M_USES_CONVERSION;
	CMByteArray	arData;
#ifndef LINUX
    long	lErr = pCard->ReadFile( pszDirName?W2A(pszDirName):NULL, pszFileName?W2A(pszFileName):NULL, arData );
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
    std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->ReadFile( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, buffer2.length() > 0 ? (char *)buffer2.c_str() : NULL, arData );
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	handleOutArray(pOutData, pOutDataSize, arData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_correctFileName			(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, EXP_LPWSTR* pOutName, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_readFile)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

    M_USES_CONVERSION;
	CMStringA	szName;
#ifndef LINUX
    long	lErr = pCard->correctFileName( pszDirName?W2A(pszDirName):NULL, pszFileName?W2A(pszFileName):NULL, szName );
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
    std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->correctFileName( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, buffer2.length() > 0 ? (char *)buffer2.c_str() : NULL, szName );
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	handleOutString(pOutName, szName, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_writeFile				(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, LPBYTE pInData, size_t inDataSize, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_writeFile)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
	CMByteArray	arData;
	setArrayData(arData, pInData, inDataSize);
#ifndef LINUX
	long	lErr = pCard->WriteFile( pszDirName?W2A(pszDirName):NULL, pszFileName?W2A(pszFileName):NULL, arData );
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
    std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->WriteFile( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, buffer2.length() > 0 ? (char *)buffer2.c_str() : NULL, arData );
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_createFile				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, long dwInitialSize, CARD_FILE_ACCESS_CONDITION ac, bool bFailIfExists, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_createFile)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
#ifndef LINUX
	long	lErr = pCard->CreateFile( pszDirName?W2A(pszDirName):NULL, pszFileName?W2A(pszFileName):NULL, dwInitialSize, ac, bFailIfExists );
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
    std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->CreateFile( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, buffer2.length() > 0 ? (char *)buffer2.c_str() : NULL, dwInitialSize, ac, bFailIfExists);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_deleteFile				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_deleteFile)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
#ifndef LINUX
	long	lErr = pCard->DeleteFile( pszDirName?W2A(pszDirName):NULL, pszFileName?W2A(pszFileName):NULL);
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
    std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->DeleteFile( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, buffer2.length() > 0 ? (char *)buffer2.c_str() : NULL);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_createDirectory			(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, bool bAdminOnly, bool bFailIfExists, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_createDirectory)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
	#ifndef LINUX
	long	lErr = pCard->CreateDirectory( pszDirName?W2A(pszDirName):NULL, bAdminOnly, bFailIfExists);
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
  //  std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
  //  std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->CreateDirectory( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, bAdminOnly, bFailIfExists);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_deleteDirectory			(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_deleteDirectory)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	M_USES_CONVERSION;
#ifndef LINUX
	long	lErr = pCard->DeleteDirectory( pszDirName?W2A(pszDirName):NULL);
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
  //  std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
  //  std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->DeleteDirectory( buffer.length() > 0 ? (char *)buffer.c_str() : NULL);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_listFiles				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPWSTR* pOutStr, DWORD* dwOutCnt, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_listFiles)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMStringArray	arFiles;

 	M_USES_CONVERSION;
 	#ifndef LINUX
 	long	lErr = pCard->ListFiles( pszDirName?W2A(pszDirName):NULL, arFiles, false);
#else
    std::basic_string<char16_t> u16Buffer(pszDirName);
  //  std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
  //  std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    long	lErr = pCard->ListFiles( buffer.length() > 0 ? (char *)buffer.c_str() : NULL, arFiles, false);
#endif
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(dwOutCnt) *dwOutCnt = (DWORD)arFiles.GetSize();
	if(pOutStr) {
		DWORD dwLen = 0;
		for(int i=0; i<arFiles.GetSize(); i++) {
			dwLen += arFiles[i].GetLength()+1;
		}
		dwLen++;
        *pOutStr = (EXP_LPWSTR)p(dwLen*sizeof(wchar_t));
		if(*pOutStr) {
			memset(*pOutStr, 0x00, dwLen*sizeof(wchar_t));
			M_USES_CONVERSION;
			int idx=0;
			for(int i=0; i<arFiles.GetSize(); i++) {
				#ifndef LINUX
				wcscpy(*pOutStr+idx, LPCTSTR2WChar(arFiles[i]));
#else
                std::string buffer(arFiles[i]);
                std::basic_string<char16_t> u16Buffer(buffer.begin(), buffer.end());
                size_t numberOfElements = u16Buffer.length();
                size_t size = u16Buffer.length() * sizeof(char16_t);
                memcpy_s(*pOutStr+idx, numberOfElements, u16Buffer.c_str(), size);
                //std::basic_string<char16_t> u16Buffer(pszReaderName);
                //std::string buffer(u16Buffer.begin(), u16Buffer.end());
                //*pOutAvail = GetCardList()->isCardAvailable(buffer.c_str());
#endif
				idx += arFiles[i].GetLength()+1;
			}
			return true;
		}
	}
	return true;
	END_TRY
}


/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_createContainer			(ICmsCoreCard hCard, BYTE bIdx, bool bKeyImport, long keySpec, long dwKeySize, LPBYTE pInKey, size_t inKeySize, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_createContainer)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMByteArrayBurned	arKey;
	setArrayData(arKey, pInKey, inKeySize);
	long	lErr = pCard->CreateContainer(bIdx, bKeyImport, keySpec, dwKeySize, pInKey?&arKey:NULL);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_deleteContainer			(ICmsCoreCard hCard, BYTE bIdx, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_deleteContainer)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	long	lErr = pCard->DeleteContainer(bIdx);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_rsaDecrypt				(ICmsCoreCard hCard, BYTE bIdx, int iKeySpec, LPBYTE pInData, size_t inDataSize, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr) 
{	BEGIN_TRY(CmsCore_CmsCard_rsaDecrypt)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMByteArray	inData, outData;
	setArrayData(inData, pInData, inDataSize);
	long	lErr = pCard->privateRsaDecrypt(bIdx, iKeySpec, inData, outData);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	handleOutArray(pOutData, pOutDataSize, outData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_readPinPolicy1			(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr) 
{	BEGIN_TRY(CmsCore_CmsCard_readPinPolicy1)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CAutoPtr<IPinPolicy>	pp(pCard->readPinPolicy(bRole));
	if(!pp) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}

	CMByteArray	arData;
	pp->toBinary(arData);
	handleOutArray(pOutData, pOutDataSize, arData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_readPinPolicy2			(ICmsCoreCard hCard, PIN_ID bRole, bool bReadTriesCounter, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr) 
{	BEGIN_TRY(CmsCore_CmsCard_readPinPolicy2)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CAutoPtr<IPinPolicy>	pp(pCard->readPinPolicy(bRole));
	if(!pp) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	if(bReadTriesCounter) {
		DWORD	dwTriesLeft=0;
		long	lErr = pCard->GetRoleTriesLeft(bRole, dwTriesLeft);
		if(lErr != CARD_ERR_SUCCESS) {
			SET_OUT_ERROR(lErr);
			return false;
		}
//		pp->set		//JD: TBD
	}

	CMByteArray	arData;
	pp->toBinary(arData);
	handleOutArray(pOutData, pOutDataSize, arData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_writePinPolicy			(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE pData, DWORD dwDataSize, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_writePinPolicy)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CMByteArray		arData;
	setArrayData(arData, pData, dwDataSize);
	CNetPinPolicy	oPolicy;
	oPolicy.fromBinary(arData, false);
	long lErr = pCard->writePinPolicy(bRole, oPolicy);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
bool	CmsCore_CmsCard_readCardProperty		(ICmsCoreCard hCard, PIN_ID bRole, DWORD dwProp, DWORD& dwOutVal, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_readCardProperty)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d, dwProp=%d"), bRole, dwProp);

	long lErr = pCard->getCardProperty(dwProp, bRole, dwOutVal);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	LOG_DEBUG_TAG(_TC("dwVal=%d"), dwOutVal);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
bool	CmsCore_CmsCard_writeCardProperty		(ICmsCoreCard hCard, PIN_ID bRole, DWORD dwProp, DWORD& dwVal, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_writeCardProperty)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d, dwProp=%d, dwVal=%d"), bRole, dwProp, dwVal);

	long lErr = pCard->setCardProperty(dwProp, bRole, dwVal);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}


/************************************************************************************************************************************************/
//public CFreeSpaces  CmsCore_CmsCard_getFreeSpaces() throws Exception;

/************************************************************************************************************************************************/
//public void     CmsCore_CmsCard_setAdminKeyIsDefault(byte[] cur_key, bool bIsDefault) throws Exception;

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getCardContainerPinList	(ICmsCoreCard hCard, bool bAddPrimaryUserPin, PIN_SET* pOut, CMSCORE_ERR* pRetErr, fkt_alloc p)
{
    std::cout << "try to get pin list container" << std::endl;
    BEGIN_TRY(CmsCore_CmsCard_getCardContainerPinList)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;
    std::cout << "card initiated " << hCard << " " << pCard << " " << bAddPrimaryUserPin << std::endl;
	PIN_SET	oData;
	long	lErr = pCard->getCardContainerPinList(oData, bAddPrimaryUserPin);
	if(lErr != CARD_ERR_SUCCESS) {
        std::cout << "return false" << std::endl;
		SET_OUT_ERROR(lErr);
		return false;
	}
    std::cout << "return true" << std::endl;
	if(pOut) *pOut = oData;
    //std::cout << "return true" << std::endl;
	return true;

	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getUnblockPinList		(ICmsCoreCard hCard, PIN_SET pinList, PIN_SET* pOut, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_getUnblockPinList)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	PIN_SET	oData=0;
	long	lErr = pCard->getUnblockPinList(pinList, oData);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(pOut) *pOut = oData;
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_PinPolicyCheckPin		(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pin, bool bConfirmed, bool bIncludeConfirm, bool* pOutResult, EXP_LPWSTR* pOutResStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_PinPolicyCheckPin)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CAutoPtr<IPinPolicy>	pp(pCard->readPinPolicy(bRole));
	if(!pp) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}

	M_USES_CONVERSION;
    CMString	szHtml;
    bool bRes = pp->validatePin(Unicode2String(pin).c_str(), bConfirmed, szHtml, bIncludeConfirm);

	if(pOutResult) *pOutResult = bRes;

	handleOutString(pOutResStr, szHtml, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getPinName				(ICmsCoreCard hCard, PIN_ID bRole, PIN_SET unblockPinList, EXP_LPWSTR* pOutName, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_getPinName)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);
	CMString	szPinName = pCard->getPinName(bRole, unblockPinList);

	handleOutString(pOutName, szPinName, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getKeySizes				(ICmsCoreCard hCard, DWORD dwKeySpec, CMSCORE_CARD_KEY_SIZES* pOutKeySizes, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_getKeySizes)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMS_CARD_KEY_SIZES	oSizes;
	long lErr = pCard->getKeySize(dwKeySpec, oSizes);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	if(pOutKeySizes) memcpy(pOutKeySizes, &oSizes, sizeof(oSizes));
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_getFreeSpaces			(ICmsCoreCard hCard, DWORD& dwBytesAvaiable, DWORD& dwKeyContainerAvailable, DWORD& dwMaxKeyContainers, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_getFreeSpaces)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	long lErr = pCard->getFreeSpace(dwBytesAvaiable, dwKeyContainerAvailable, dwMaxKeyContainers);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_SSO_Set					(ICmsCoreCard hCard, PIN_ID bRole, bool bEnabled, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_SSO_Set)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d, bEnabled=%s"), bRole, LOG_BOOL(bEnabled));

	CAutoPtr<IPinPolicy>	pp(pCard->readPinPolicy(bRole));
	if(!pp) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	CNetPinPolicy*			oPolicy = dynamic_cast<CNetPinPolicy*>(&*pp);
	if(!oPolicy) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	oPolicy->setSsoSupported(bEnabled);
	long lErr = pCard->writePinPolicy(bRole, *oPolicy);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}

	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_SSO_Get					(ICmsCoreCard hCard, PIN_ID bRole, bool* bOutEnabled, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_SSO_Get)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("role=%d"), bRole);

	CAutoPtr<IPinPolicy>	pp(pCard->readPinPolicy(bRole));
	if(!pp) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	CNetPinPolicy*			oPolicy = dynamic_cast<CNetPinPolicy*>(&*pp);
	if(!oPolicy) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}
	if(bOutEnabled)
		*bOutEnabled = oPolicy->getSsoSupported();
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
#ifdef WINDOWS
static bool getRootCerts(IMinidriverCard* pCard, HCERTSTORE& hOutRootsStore, DWORD& dwErr)
#else
static bool getRootCerts(IMinidriverCard* pCard, CMS_ContentInfo* hOutRootsStore, DWORD& dwErr)
#endif
{
	CMArray<BYTE, BYTE>	arRoots;
	CMiniDriverUtils		md_utils;
	if(!md_utils.readRootCerts(pCard, arRoots, &dwErr)) 
		return false;

    CCapiCertUtil::BlobToStore(arRoots, &hOutRootsStore, &dwErr);
	return true;
}

#ifdef WINDOWS
static bool getRootCert(HCERTSTORE& hRootsStore, int idx, CMByteArray& arOutCert, DWORD* pRetErr)
#else
static bool getRootCert(CMS_ContentInfo* hRootsStore, int idx, CMByteArray& arOutCert, DWORD* pRetErr)
#endif
{
    return CCapiCertUtil::GetCertFromStore(hRootsStore, idx, arOutCert, pRetErr);
}

static long getContainerCert(IMinidriverCard*	pCard, CONTAINER_ID id, DWORD dwFlags, CNetCardContainer&	oCont, CKeyContainerCert*	cc, bool& bOutCertOnly)
{
	int		idx			= CONTAINER_ID_IDX(id);
	int		iKeySpec	= CONTAINER_ID_KEYSPEC(id);

	if(CONTAINER_ID_ISROOTCERT(id)) 
	{
		bOutCertOnly=true;
#ifdef WINDOWS
        HCERTSTORE          m_hRootsStore=NULL;
#else
        CMS_ContentInfo*	m_hRootsStore=NULL;
#endif
		DWORD dwErr = 0;
		if(!getRootCerts(pCard, m_hRootsStore, dwErr))
			return dwErr;

		CMByteArray	arCert;
		if(!getRootCert(m_hRootsStore, idx, arCert, &dwErr))
			return dwErr;

		if(cc)
			cc->setUncompressedCertificate(arCert);

        CCapiCertUtil::CertCloseStore(&m_hRootsStore, CERT_CLOSE_STORE_CHECK_FLAG);
        return CARD_ERR_SUCCESS;
    }
	else
	{
		bOutCertOnly=false;
		long	lErr = pCard->GetCountainer(idx, oCont, dwFlags);
		if(lErr != CARD_ERR_SUCCESS) 
			return lErr;
		
		if(cc) {
			for(UINT j=0; j<oCont.getCertCnt(); j++) {
				if(oCont.getCert(j, *cc)) {
					if(cc->getKeySpec() == iKeySpec)
						return CARD_ERR_SUCCESS;
				}
			}
		}
	}
	return CARD_ERR_NOT_FOUND;
}

/************************************************************************************************************************************************/

CmsCore_API bool	CmsCore_CmsCard_ContainersGet			(ICmsCoreCard hCard, CONTAINER_ID** pOutIds, DWORD* pOutSize, fkt_alloc p, ICmsCoreProgress* pProgress, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersGet)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	CMArray<CONTAINER_ID, CONTAINER_ID>	arCont;
	long	lCnt=0;
	if(pCard->GetContainerCount(lCnt) == CARD_ERR_SUCCESS) 
	{
		if(pProgress) {
			pProgress->SetRange(0, (short)(lCnt+2));
			pProgress->SetPos(1);
#ifndef __GNUC__
			pProgress->Progress(L"Reading certificates ...");
#else
			std::string bufferString("Reading certificates ...");
            std::basic_string<char16_t> buffer(bufferString.begin(), bufferString.end());
            pProgress->Progress(buffer.c_str());
#endif
		}

		for(int i=0; i<lCnt; i++) 
		{
			if(pProgress) pProgress->StepIt();
			CNetCardContainer	oCont;
			if(pCard->GetCountainer(i, oCont, READ_CONTAINER_NONE/*READ_CONTAINER_CERT|READ_CONTAINER_PININFO*/) == CARD_ERR_SUCCESS) {
				for(UINT j=0; j<oCont.getCertCnt(); j++) {
					CKeyContainerCert	cc;
					if(oCont.getCert(j, cc)) {
						DWORD	dwId = (CONTAINER_ID)MAKE_CONTAINER_ID(cc.getKeySpec(), i);
						arCont.Add(dwId);
					}
				}
			}
		}
	}

	{	//get Root certs
#ifdef WINDOWS
        HCERTSTORE          m_hRootsStore=NULL;
#else
        CMS_ContentInfo*    m_hRootsStore=NULL;
#endif
		DWORD dwErr = 0;
		if(!getRootCerts(pCard, m_hRootsStore, dwErr)) {
			LOG_ERROR_TAG(_TC("no root certs"));
// 			SET_ERROT(
// 			return false;
		} else {
			int	i=0;
			CMByteArray	arCert;
			while(getRootCert(m_hRootsStore, i, arCert, &dwErr)) {
				DWORD	dwId = (CONTAINER_ID)MAKE_CONTAINER_ID(CID_ROOT_CERT, i);
				arCont.Add(dwId);
                i++;
			}
            CCapiCertUtil::CertCloseStore(&m_hRootsStore, CERT_CLOSE_STORE_CHECK_FLAG);
		}
	} //get Root certs


	if(!arCont.GetSize()) {
		SET_OUT_ERROR(CARD_ERR_NOT_FOUND);
		return false;
	}

	if(pOutIds) {
		*pOutIds = (CONTAINER_ID*)p(arCont.GetSize()*sizeof(CONTAINER_ID));
		for(int i=0; i<arCont.GetSize(); i++)
			*((*pOutIds)+i) = arCont[i];
	}
	if(pOutSize) *pOutSize = (DWORD)arCont.GetSize();
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersGetLabel		(ICmsCoreCard hCard, CONTAINER_ID contId, int iWhich, EXP_LPWSTR* pOutResStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersGetLabel)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	CNetCardContainer	oCont;
	CKeyContainerCert	cc;
	bool				bCertOnly=false;
	long lErr = getContainerCert(pCard, contId, READ_CONTAINER_ALL, oCont, &cc, bCertOnly);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	CMByteArray	arData;
	if(!cc.getUncompressedCertificate( arData )) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	CMString	szLabel;

	if((iWhich == 1) || (iWhich == 2) || (iWhich == 3)) {		//from cert
		CMArray<BYTE, BYTE>	cert;
		CMString				sValidFrom, sValidTo, sDefault, sContainer, sKeySpec, sPin, sKeySize;
		CMStringW				outSubject, outSubjectFull, outIssuerCN, outIssuerFull, outSerial, outSerialFull, outFingerprint;
		FILETIME				outExpiration = {0,0}, outValidFrom = {0,0};

		if(cc.getUncompressedCertificate(cert)) {
			if(CCapiCertUtil::getCertInfoW(cert, outSubjectFull, outIssuerFull, outSerial, outFingerprint, outValidFrom, outExpiration)) {
				if(iWhich == 1) {		//issued to
                    szLabel = WChar2Utf8(CCapiCertUtil::getCN_W(outSubjectFull));
				}
				if(iWhich == 2) {		//issued by
                    szLabel  = WChar2Utf8(CCapiCertUtil::getCN_W(outIssuerFull));
				}
				if(iWhich == 3) {		//expiration date
					SYSTEMTIME systemTime;
					VERIFY(::FileTimeToSystemTime(&outExpiration, &systemTime));						//getSystemTimeString
					szLabel.Format(_TC("%02ld.%02ld.%04ld %02ld:%02ld:%02ld"), 
						systemTime.wDay, systemTime.wMonth, systemTime.wYear, systemTime.wHour, systemTime.wMinute, systemTime.wSecond);
				}
			} 
		}
	}

	if(iWhich == 4) {		//PIN
		if(!bCertOnly)
			szLabel = pCard->getRolePinPurposeString(oCont.getPinId());
	}

	handleOutString(pOutResStr, szLabel, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersGetCert		(ICmsCoreCard hCard, CONTAINER_ID contId, LPBYTE* pOutCert, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersGetCert)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	CNetCardContainer	oCont;
	CKeyContainerCert	cc;
	bool				bCertOnly=false;
	long lErr = getContainerCert(pCard, contId, READ_CONTAINER_CERT, oCont, &cc, bCertOnly);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	CMByteArray	arData;
	if(!cc.getUncompressedCertificate( arData )) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	handleOutArray(pOutCert, pOutDataSize, arData, p);
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersGetIsDefault	(ICmsCoreCard hCard, CONTAINER_ID contId, bool* pOutIsDefault, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersGetIsDefault)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	CNetCardContainer	oCont;
	CKeyContainerCert	cc;
	bool				bCertOnly=false;
	long lErr = getContainerCert(pCard, contId, READ_CONTAINER_NONE, oCont, &cc, bCertOnly);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(!bCertOnly) {
		if(pOutIsDefault) *pOutIsDefault = oCont.isDefault();
	} else {
		if(pOutIsDefault) *pOutIsDefault = false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersShowCert		(ICmsCoreCard hCard, CONTAINER_ID contId, HWND hParentWnd, EXP_LPCWSTR pszTitle, bool bEnableImport, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersShowCert)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	CNetCardContainer	oCont;
	CKeyContainerCert	cc;
	bool				bCertOnly=false;
	long lErr = getContainerCert(pCard, contId, READ_CONTAINER_ALL, oCont, &cc, bCertOnly);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	CMByteArray	arData;
	if(!cc.getUncompressedCertificate( arData )) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}

	ICardReconnector oo(pCard);

	M_USES_CONVERSION;
#ifndef LINUX
    CCapiCertUtil::showCert(hParentWnd, arData, WChar2LPCTSTR(pszTitle), bEnableImport);
#else
    std::basic_string<char16_t> u16Buffer(pszTitle);
  //  std::basic_string<char16_t> u16Buffer2(pszFileName);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
  //  std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    CCapiCertUtil::showCert(hParentWnd, arData, buffer.c_str(), bEnableImport);
#endif
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersSetIsDefault	(ICmsCoreCard hCard, CONTAINER_ID contId, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersSetIsDefault)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	long lErr = pCard->SetDefaultContainer(CONTAINER_ID_IDX(contId));
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersDelete		(ICmsCoreCard hCard, CONTAINER_ID contId, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersDelete)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("contId=%x"), contId);

	int		idx			= CONTAINER_ID_IDX(contId);
	int		iKeySpec	= CONTAINER_ID_KEYSPEC(contId);

	if(CONTAINER_ID_ISROOTCERT(contId)) 
	{
#ifdef WINDOWS
        HCERTSTORE          m_hRootsStore=NULL;
#else
        CMS_ContentInfo*    m_hRootsStore=NULL;
#endif
		DWORD dwErr = 0;
		if(!getRootCerts(pCard, m_hRootsStore, dwErr)) {
			SET_OUT_ERROR(dwErr);
			return false;
		}

        bool	bDeleted = CCapiCertUtil::DelCertInStore(&m_hRootsStore, idx);

		if(bDeleted) {
			CMByteArray	arRoots;
			if(CCapiCertUtil::StoreToBlob(m_hRootsStore, arRoots)) {
				CMiniDriverUtils		md_utils;
				DWORD					dwErr;
				if(!md_utils.writeRootCerts(pCard, arRoots, true, &dwErr)) {
					SET_OUT_ERROR(dwErr);
					return false;
				}
			}
		}

        CCapiCertUtil::CertCloseStore(&m_hRootsStore, CERT_CLOSE_STORE_CHECK_FLAG);
	}
	else
	{
		long lErr = pCard->DelContainer(idx, iKeySpec);
		if(lErr != CARD_ERR_SUCCESS) {
			SET_OUT_ERROR(lErr);
			return false;
		}
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersDeleteAll		(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersDeleteAll)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	long	lCnt=0;
	bool	bRet=true;
	if(pCard->GetContainerCount(lCnt) == CARD_ERR_SUCCESS) 
	{
		bool	bRet=false;
		for(int i=lCnt-1; i>=0; i--) 
		{
			CNetCardContainer	oCont;
			if(pCard->GetCountainer(i, oCont, READ_CONTAINER_NONE/*READ_CONTAINER_CERT|READ_CONTAINER_PININFO*/) == CARD_ERR_SUCCESS) {
				CMString	szName=oCont.getName();
				long lErr = pCard->DelContainer(i, 0);
				if(lErr == CARD_ERR_SUCCESS) {
                    LOG_DEBUG_TAG(_TC("del: [%d] %s. ok"), i, (LPCTSTR)szName);
				} else {
					bRet=false;
					SET_OUT_ERROR(lErr);
                    LOG_DEBUG_TAG(_TC("del: [%d] %s. failed"), i, (LPCTSTR)szName);
				}
			}
		}
	}
	long lErr = pCard->DeleteFile("mscp", "msroots");
	if(lErr != CARD_ERR_SUCCESS) {
		bRet=false;
		SET_OUT_ERROR(lErr);
	}
	pCard->GetBasicPcsc()->getCache().clear();	//to clear e.g. mapfile
	pCard->GetBasicPcsc()->setLastWrite();

	return bRet;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_ContainersImportCert	(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pszRolePin, EXP_LPCWSTR pszFilename, EXP_LPCWSTR pszPin, CONTAINER_ID contId, int iKeySpec, EXP_LPWSTR* pOutErrStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersImportCert)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	LOG_DEBUG_TAG(_TC("file=%ws, pin=%s, contId=%x, iKeySpec=%d"), pszFilename, pszPin?_TC("..."):_TC("null"), contId, iKeySpec);

	CMString	szContainerName;
	if(contId) {
		CNetCardContainer	oCont;
		bool				bCertOnly=false;
		long lErr = getContainerCert(pCard, contId, READ_CONTAINER_NONE, oCont, NULL, bCertOnly);
		if(lErr != CARD_ERR_SUCCESS) {
			SET_OUT_ERROR(lErr);
			return false;
		}
		szContainerName = oCont.getName();
	}
	M_USES_CONVERSION;
	CMiniDriverUtils	oo;
    CErrorStackNew		oErr;/*
#ifndef LINUX
    if(!oo.importCertKeyToSmartCard(pCard, bRole, WChar2LPCTSTR(pszRolePin), NULL, WChar2LPCTSTR(pszFilename), WChar2LPCTSTR(pszPin), szContainerName, iKeySpec, true, false, true, true, false, NULL, &dwErr)) {
#else
    std::basic_string<char16_t> u16Buffer(pszRolePin);
    std::basic_string<char16_t> u16Buffer2(pszFilename);
    std::basic_string<char16_t> u16Buffer3(pszPin);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    std::string buffer3(u16Buffer3.begin(), u16Buffer3.end());
    if(!oo.importCertKeyToSmartCard(pCard, bRole, buffer.c_str(), NULL, buffer2.c_str(), buffer3.c_str(), szContainerName, iKeySpec, true, false, true, true, false, NULL, &oErr)) {
#endif
		if(!oErr.find(CARD_ERR_ABORTED)) {
			SET_OUT_ERROR(oErr.getErrorCode());
			return false;
		}
    }*/
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
bool	CmsCore_CmsCard_ContainersImportCerts	(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pszRolePin, LPBYTE pbCertData, DWORD dwCertDataSize, DWORD& dwOutAddedCnt, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_ContainersImportCerts)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	dwOutAddedCnt=0;

	if(!pbCertData)
	{
		SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
		return false;
	}
	if(!dwCertDataSize)
		return true;

	bool		bAdded=false;
#ifdef WINDOWS
    HCERTSTORE          m_hRootsStoreCard=NULL;
    HCERTSTORE          m_hRootsToImport=NULL;
    HCERTSTORE          _hRootsToImport=NULL;
#else
    CMS_ContentInfo*	m_hRootsStoreCard=NULL;
    CMS_ContentInfo*	m_hRootsToImport=NULL;
    CMS_ContentInfo*	_hRootsToImport=NULL;
#endif
	DWORD dwErr = 0;
	bool  bRet=true;
	if(!CCapiCertUtil::BlobToStore(pbCertData, dwCertDataSize, &m_hRootsToImport)) 
	{
		SET_OUT_ERROR(GetLastError()|CARD_ERR_GRP_WINDOWS);
		return false;
	}

	if(getRootCerts(pCard, m_hRootsStoreCard, dwErr)) {	//Merge
        _hRootsToImport = m_hRootsStoreCard;
        bRet = CCapiCertUtil::AddCertToStore(m_hRootsToImport, m_hRootsStoreCard, dwOutAddedCnt, &dwErr);
/*
		PCCERT_CONTEXT	pCC(NULL);
		int				i=0;
		bRet=true;
		do								//Add now all new one
		{
			pCC = CertEnumCertificatesInStore(m_hRootsToImport, pCC);
			if(pCC == NULL) 
				break;
			if(!CertAddCertificateContextToStore (m_hRootsStoreCard, pCC, CERT_STORE_ADD_NEW, NULL)) {
				DWORD dwErr = ::GetLastError();
				if(dwErr != CRYPT_E_EXISTS) {
					if(pRetErr) *pRetErr = CARD_ERR_GRP_WINDOWS|dwErr;
					bRet=false;
				}
			} else {
				dwOutAddedCnt++;
				bAdded=true;
			}
			i++;
		} while(true);
*/
	} else {	//Just add
        _hRootsToImport = m_hRootsToImport;
        bRet = CCapiCertUtil::GetCertCntInStore(_hRootsToImport, dwOutAddedCnt, &dwErr);
        bRet=true;
        bAdded=true;
/*
		bRet=true;
		bAdded=true;
		_hRootsToImport = m_hRootsToImport;
		PCCERT_CONTEXT	pCC(NULL);
		do								//Add now all new one
		{
			pCC = CertEnumCertificatesInStore(m_hRootsToImport, pCC);
			if(pCC == NULL) 
				break;
			dwOutAddedCnt++;
		} while(true);
*/
	}

	if(bAdded) {
		CMByteArray	arRoots;
		if(CCapiCertUtil::StoreToBlob(_hRootsToImport, arRoots)) {
			CMiniDriverUtils		md_utils;
			DWORD					dwErr;
			if(!md_utils.writeRootCerts(pCard, arRoots, true, &dwErr)) {
				LOG_ERROR_TAG(_TC("write failed %08lx"), dwErr);
				SET_OUT_ERROR(dwErr);
				return false;
			}
		}
	}

    CCapiCertUtil::CertCloseStore(&m_hRootsStoreCard, CERT_CLOSE_STORE_CHECK_FLAG);
    CCapiCertUtil::CertCloseStore(&m_hRootsToImport, CERT_CLOSE_STORE_CHECK_FLAG);

	return bRet;
	END_TRY
}

/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_readPinInfo				(ICmsCoreCard hCard, PIN_ID bRole, PIN_INFO* pOutData, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_readPinInfo)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	PIN_INFO	pinInfo = { PIN_INFO_CURRENT_VERSION };
	long	lErr = pCard->GetPinInfo( bRole, pinInfo);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	if(pOutData) {
		if(pOutData->dwVersion != pinInfo.dwVersion) {
			SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
			return false;
		}
        memcpy(pOutData, &pinInfo, sizeof(pinInfo));
	}
	return true;
	END_TRY
}


/************************************************************************************************************************************************/
CmsCore_API bool	CmsCore_CmsCard_writePinInfo			(ICmsCoreCard hCard, PIN_ID bRole, PIN_INFO* pInData, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_CmsCard_writePinInfo)
	IMinidriverCard*	pCard;
	if(!initCard(hCard, pCard, pRetErr, p)) return false;

	if(!pInData) 
	{
		SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
		return false;
	}

	long	lErr = pCard->SetPinInfo( bRole, *pInData);
	if(lErr != CARD_ERR_SUCCESS) {
		SET_OUT_ERROR(lErr);
		return false;
	}
	return true;
	END_TRY
}

/************************************************************************************************************************************************/
#include "global/helpers/UpdateChecker.h"
class CUpdateCheckerNotificationSimple : public IUpdateCheckerNotification {
public:
	CUpdateCheckerNotificationSimple()	{ }
	virtual bool	OnValueBegin(LPCTSTR pszRootNode, UINT iFieldCnt)	{ return true; }
	virtual bool	OnValue(LPCTSTR pszRootNode, LPCTSTR pszNodeName, LPCTSTR pszValue, DWORD& dwSkipNextTime, DWORD& dwDisableAutocheck);
	virtual bool	OnValueEnd(LPCTSTR pszRootNode)						{ return true; }
	CMString	m_szMsg;
};

bool	CUpdateCheckerNotificationSimple::OnValue(LPCTSTR pszRootNode, LPCTSTR pszNodeName, LPCTSTR pszValue, DWORD& dwSkipNextTime, DWORD& dwDisableAutocheck) {
	CMString	str;
	str.Format(_TC("/%s/msg"), pszRootNode);
	if(_tcsicmp(str, pszNodeName))
		return true;

	CMByteArray	arr;
	ConvertB64StringToByteArray(pszValue, arr);

	LOG_DEBUG_TAG(_TC("update detected"));

	m_szMsg = ConvertByteArrayToCMString(arr);
	return false;
}


CmsCore_API bool	CmsCore_CheckForUpdates					(EXP_LPCWSTR pszUrl, EXP_LPCWSTR pszXmlNode, bool& bOutUpdateAvailable, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CheckForUpdates)

    return CmsCore_CheckForUpdates1(NULL, pszUrl, pszXmlNode, bOutUpdateAvailable, pOutMsgStr, p, pRetErr);
    END_TRY
}


CmsCore_API bool	CmsCore_CheckForUpdates1				(IHttpSend* pHttpSend, EXP_LPCWSTR pszUrl, EXP_LPCWSTR pszXmlNode, bool& bOutUpdateAvailable, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CheckForUpdates1)

    M_USES_CONVERSION;
    CUpdateCheckerNotificationSimple		oResult;
#ifndef LINUX
    CUpdateChecker		m_UpdateChecker(WChar2LPCTSTR(pszUrl), WChar2LPCTSTR(pszXmlNode), (ICfgReadWrite*)NULL);
#else
    std::basic_string<char16_t> u16Buffer(pszUrl);
    std::basic_string<char16_t> u16Buffer2(pszXmlNode);
    //std::basic_string<char16_t> u16Buffer3(pszPin);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    //std::string buffer3(u16Buffer3.begin(), u16Buffer3.end());
     CUpdateChecker		m_UpdateChecker(buffer.c_str(), buffer2.c_str(), (ICfgReadWrite*)NULL);
#endif

    m_UpdateChecker.setHttpSend(pHttpSend);

// 	m_UpdateChecker(_TC("http://www.versasec.com/updates/vsec.cms.t.o.php?ver=<ver>&lic=<lic>&tid=<tid>"), _TC("vsec_cms_t_o"), &m_UpdateCheckerCfg, UPDATE_DAILY),

    m_UpdateChecker.doCheck(NULL, &oResult, true);

    if(oResult.m_szMsg.IsEmpty()) {
        bOutUpdateAvailable=false;
    } else {
        bOutUpdateAvailable=true;
        handleOutString(pOutMsgStr, oResult.m_szMsg, p);
    }
    return true;
    END_TRY
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static bool initP12(DWORD dwInst, CP12Parser*& pP12, CMSCORE_ERR* pRetErr, fkt_alloc p)
{
	CMSingleLock	_sl(&m_cs_P12, true);
	if(m_P12.find(dwInst) == m_P12.end()) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	if(!m_P12[dwInst]) {
		SET_OUT_ERROR(CARD_ERR_INTERNAL);
		return false;
	}
	pP12 = m_P12[dwInst];
	return true;
}

CmsCore_API bool	CmsCore_P12Parse						(EXP_LPCWSTR pszP12File, EXP_LPCWSTR pszPwd, DWORD& dwOutInst, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_P12Parse)
	CMSingleLock	_sl(&m_cs_P12, true);
	CAutoPtr<CP12Parser>	oParser(new CP12Parser());
	DWORD					dwErr;
	M_USES_CONVERSION;
#ifndef LINUX
	if(!oParser->parse(W2A(pszP12File), W2A(pszPwd), &dwErr)) {
#else
    std::basic_string<char16_t> u16Buffer(pszP12File);
    std::basic_string<char16_t> u16Buffer2(pszPwd);
    //std::basic_string<char16_t> u16Buffer3(pszPin);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    //std::string buffer3(u16Buffer3.begin(), u16Buffer3.end());
    if(!oParser->parse(buffer.c_str(), buffer2.c_str(), &dwErr)) {
#endif
		SET_OUT_ERROR(dwErr);
		return false;
	}
	
	dwOutInst = ::GetTickCount();
	m_P12[dwOutInst] = oParser.Detach();
	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_P12GetCnt						(DWORD	dwInst, DWORD& dwOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_P12GetCnt)
	CP12Parser*	pP12;
	if(!initP12(dwInst, pP12, pRetErr, p)) return false;

	dwOutCnt = pP12->getCertCnt();
	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_P12GetLabel						(DWORD	dwInst, DWORD dwIdx, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_P12GetCnt)
	CP12Parser*	pP12;
	if(!initP12(dwInst, pP12, pRetErr, p)) return false;

	CMString	str = pP12->getCertLabel(dwIdx);
	handleOutString(pOutMsgStr, str, p);
	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_P12MarkToDelete					(DWORD	dwInst, DWORD dwIdx, CMSCORE_ERR* pRetErr, fkt_alloc p)
{	BEGIN_TRY(CmsCore_P12GetCnt)
	CP12Parser*	pP12;
	if(!initP12(dwInst, pP12, pRetErr, p)) return false;

	pP12->markToDelete(dwIdx);
	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_P12GetCert						(DWORD	dwInst, LPBYTE* pOutCerts, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_P12GetCnt)
	CP12Parser*	pP12;
	if(!initP12(dwInst, pP12, pRetErr, p)) return false;

	CMByteArray	arData;
	DWORD		dwErr;
	if(!pP12->getRootCertAsBlob(arData, &dwErr)) {
		CMSingleLock	_sl(&m_cs_P12, true);
		delete m_P12[dwInst];
		m_P12.erase(dwInst);
		SET_OUT_ERROR(dwErr);
		return false;
	}
	handleOutArray(pOutCerts, pOutDataSize, arData, p);
	CMSingleLock	_sl(&m_cs_P12, true);
	delete m_P12[dwInst];
	m_P12.erase(dwInst);
	return true;
	END_TRY
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
bool g_bDidGenLicenseUpgradeChallenge=false;

CmsCore_API bool	CmsCore_GenLicenseUpgradeChallenge		( BYTE bWhich, EXP_LPWSTR* pOutMsgStr,fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_GenLicenseUpgradeChallenge)

	readActivationChallenges();			//Ensure loading
	CMString str = GetGlobalConfig().GetSTRING(VSCFG_BY_PARAM(_TC("/license/setup")));

	CMByteArray	arRnd;
	if(!g_bDidGenLicenseUpgradeChallenge) {
		g_bDidGenLicenseUpgradeChallenge=true;
		getRandom(arRnd, 3, true);
	} else {
		getRandom(arRnd, 3, false);
	}

	{
		CMSingleLock	_sl(&g_cs_Rnd, true);
		CMString	szRnd = ConvertByteArrayToHexString(arRnd);	g_Rnd.Add(szRnd);
		storeActivationChallenges();
	}
	arRnd.SetSize(4);
	arRnd[3]=LOBYTE(_ttoi(str));
	if(bWhich)
		arRnd[3] = bWhich;

	CMByteArray	arData;
    copyArray(arData, arRnd);
	CVS_MK	k;
	if(!sec_TDESEncrypt(k.GetKey(), (LPBYTE)"\x71\x25", 2, 1, arData, 1)) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	str = ConvertByteArrayToHexString(arData);
	handleOutString(pOutMsgStr, str, p);
	return true;
	END_TRY
}

CmsCore_API bool	CmsCore_ApplyLicenseUpgradeResponse		(EXP_LPCWSTR pszResp, bool bInstall, bool& bOk, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_ApplyLicenseUpgradeResponse)

	readActivationChallenges();			//Ensure loading

	bOk=false;
	CMSingleLock	_sl(&g_cs_Rnd, true);
	if(g_Rnd.GetSize()==0)
	{
		SET_OUT_ERROR(CARD_ERR_NULL_CHALLENGE);
		return false;
	}

	M_USES_CONVERSION;
	CMByteArray	arData;
#ifndef __GNUC__
    ConvertHexStringToByteArray(WChar2LPCTSTR(pszResp), arData);
#else
    std::basic_string<char16_t> u16Buffer(pszResp);
    //std::basic_string<char16_t> u16Buffer2(pszPwd);
    //std::basic_string<char16_t> u16Buffer3(pszPin);
    std::string buffer(u16Buffer.begin(), u16Buffer.end());
    //std::string buffer2(u16Buffer2.begin(), u16Buffer2.end());
    //std::string buffer3(u16Buffer3.begin(), u16Buffer3.end());
    ConvertHexStringToByteArray(buffer.c_str(), arData);
#endif
	CVS_MK	k;
	if(!sec_TDESDecrypt(k.GetKey(), (LPBYTE)"\x52\x17", 2, 1, arData, 1)) {
		SET_OUT_ERROR(CARD_ERR_OUT_OF_RESOURCE);
		return false;
	}
	CMString	szRnd = ConvertByteArrayToHexString(&arData[0], 3);	
	int idx = CheckStringIsIn(g_Rnd, szRnd, true, false);
	if(idx<0) {
		LOG_DEBUG_TAG(_TC("cnt_codes=%d"), g_Rnd.GetSize());
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	g_Rnd.RemoveAt(idx);
	storeActivationChallenges();
//	ConvertHexStringToByteArray(_TC("01020303"), arData);

	CMString	str;
	str.Format(_TC("%d"), arData[3]);
	handleOutString(pOutMsgStr, str, p);

	if(bInstall)
	{
		CMString			szCfgFile = GetGlobalConfigMainFileName();
		CXmlConfigProvider	oCfg;
		if(oCfg.LoadXmlFile(szCfgFile, VSCFG_BASEPATH_TEMP, true)) {
			CMString	strCode = calcLicenseCode();
			oCfg.SetSetting(_TC("/oem/force/license/code"), (LPBYTE)LPCTSTR2ConstChar(strCode), strCode.GetLength(), IConfig::tString, 0, NULL);
			strCode.Format(_TC("%d"), arData[3]);
			oCfg.SetSetting(_TC("/oem/force/license/setup"), (LPBYTE)LPCTSTR2ConstChar(strCode), strCode.GetLength(), IConfig::tString, 0, NULL);
		}

		//TBD:
		//Write node to 

		CMByteArray	arPrivKey;
		CMString	sPrvKey = 
#include "keys/gto.pvk.bin"
		int  len=sPrvKey.GetLength();
		arPrivKey.SetSize(len);
		for (int i = 0; i < len; i++) arPrivKey[i] = (BYTE)(255 - (BYTE)(sPrvKey[i]));

		CXMLSignature	oXMLSignature(arPrivKey);
		CMString			oSignature;

		//Signing 
		long lRet = oXMLSignature.SignXMLNode( szCfgFile, VSCFG_BASEPATH_UNSIGNED _T("/force"), "signature", ""/*params.szKeyFile*/, ""/*params.szPin*/, oSignature, 1);
		if ( (lRet == 0) )
		{
//			printf("Signing of '%s' was successfull", params.szCfgFile);
		} else {
//			printf("Signing failed!!!\n");
		}
#ifdef AfxGetvsCfgLogApp
        AfxGetvsCfgLogApp()->ReInitializeConfigFile();
#endif
	}

	bOk=true;
	return true;
	END_TRY
}

/* It does in fact code generation */
CmsCore_API bool	CmsCore_CheckLicenseUpgradeChallenge		(T_LICCHECK *pStruct, BYTE bVal, fkt_alloc p, CMSCORE_ERR* pRetErr)
{	BEGIN_TRY(CmsCore_CheckLicenseUpgradeChallenge)

	if(!p) {
		SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
		return false;
	}
	if(!pStruct->pInData || !pStruct->dwInDataSize) {
		SET_OUT_ERROR(CARD_ERR_WRONG_PARAM);
		return false;
	}

	CMByteArray	arInData, arRespData;
	setArrayData(arInData, pStruct->pInData, pStruct->dwInDataSize);
	
	CVS_MK	k;
	if(!sec_TDESDecrypt(k.GetKey(), (LPBYTE)"\x71\x25", 2, 1, arInData, 1)) {
		SET_OUT_ERROR(CARD_ERR_OBJECT_INVALID);
		return false;
	}
	arRespData.SetSize(4);
	memcpy(&arRespData[0], &arInData[0], 3);
	arRespData[3] = bVal;
	if(!sec_TDESEncrypt(k.GetKey(), (LPBYTE)"\x71\x25", 2, 1, arRespData, 1)) {
		SET_OUT_ERROR(CARD_ERR_INTERNAL);
		return false;
	}

	handleOutArray(&pStruct->pOutData, &pStruct->dwOutDataSize, arRespData, p);
	return true;
	END_TRY
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Localization specific
CmsCore_API bool		CmsCore_Init_Localization(HCMSCOREINST hInst, EXP_LPWSTR languageId, EXP_LPCWSTR pszLocFileName, DWORD dwFlags, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool		CmsCore_End_Localization(HCMSCOREINST hInst, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool		CmsCore_Translate(EXP_LPCWSTR pStringToTranslate, EXP_LPCWSTR trType, EXP_LPCWSTR pszSrcFile, int iLine, EXP_LPWSTR* pOutTranslatedString, fkt_alloc p, CMSCORE_ERR* pRetErr) {
    std::string	str = Unicode2String(pStringToTranslate);
    std::for_each(str.begin(), str.end(), [](char & c){
        c = ::toupper(c);
    });
    handleOutString(pOutTranslatedString, str.c_str(), p);
	return true;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////  IUserAuth  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if (0)
/* asks server what features the user can authenticate for, so we can show correct UI */
CmsCore_API bool	CmsCore_UserAuth_supportedFeatures(EXP_LPCWSTR  pszCSN, /*requestedFeatureAccess&*/DWORD* featureAccess, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* asks server what pending tasks are there for a specific card (e.g. DAS upgrade) */
CmsCore_API bool	CmsCore_UserAuth_getPendingTasks(EXP_LPCWSTR  pszCSN, /*pendingTasks&*/DWORD* oPendingTasks, CAutoPtr<IServerCommSettings>& outSettings, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* run a task for a specific card (e.g. DAS upgrade) */
CmsCore_API bool	CmsCore_UserAuth_runTask(EXP_LPCWSTR  pszCSN, /*pendingTasks*/DWORD oTasks, CMByteArray& arInData, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* asks server to return a key (e.g. to be used for data encryption going forward) */
CmsCore_API bool	CmsCore_UserAuth_requestKey(EXP_LPCWSTR  pszCSN, IKeyRequestData& inKeyReq, CAutoPtr<IKeyResponseData>& outKeyResp, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* asks server what auth method he supports based on what the user can provide and what features he wants to access */
CmsCore_API bool	CmsCore_UserAuth_requestAuth(EXP_LPCWSTR  pszCSN, /*requestedFeatureAccess*/DWORD featureAccess, /*authMethod*/DWORD preferedMeth, /*authMethod&*/DWORD* supportedMethod, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* client initiates authentication flow. server returns a challenge and a list of keys he will accept */
CmsCore_API bool	CmsCore_UserAuth_initAuth(EXP_LPCWSTR  pszCSN, /*requestedFeatureAccess*/DWORD featureAccess, /*authMethod*/DWORD usedMethod, IIssueSrvAuthKeys& existingKeys, CAutoPtr<IAuthChallenge>& outAuthChallenge, CAutoPtr<IIssueSrvAuthKeys>& outSupportedKeys, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* client signs the challenge with one of his keys and send the token back to server. Server then response a session key, which will be used to sign each further request */
CmsCore_API bool	CmsCore_UserAuth_authenticate(EXP_LPCWSTR  pszCSN, IAuthToken& authToken, CAutoPtr<IAuthSessionKey>& outSessionKey, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* check if the current user is authenticated and for what features he will have access to with current login level */
CmsCore_API bool	CmsCore_UserAuth_isAuthenticated(EXP_LPCWSTR  pszCSN, /*requestedFeatureAccess&*/DWORD* outEnabledFeatures, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool	CmsCore_UserAuth_logout(EXP_LPCWSTR  pszCSN, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* update server auth keys */
CmsCore_API bool	CmsCore_UserAuth_updateAuth(EXP_LPCWSTR  pszCSN, IAuthUserPwd& newPwd, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool	CmsCore_UserAuth_updateAuth(EXP_LPCWSTR  pszCSN, IIssueSrvAuthKeys& newKeys, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool	CmsCore_UserAuth_updateAuth(EXP_LPCWSTR  pszCSN, EXP_LPCWSTR  pszOtherCSN, IIssueSrvAuthKeys& newKeys, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool	CmsCore_UserAuth_genUserPwd(EXP_LPCWSTR  pszCSN, EXP_LPCWSTR  pszUserCSN, CAutoPtr<IAuthUserPwd>& outNewPwd, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

CmsCore_API bool	CmsCore_UserAuth_genUserPwd(EXP_LPCWSTR  pszCSN, EXP_LPCWSTR  pszUserCSN, DWORD dwTemplateId, CAutoPtr<IAuthUserPwd>& outNewPwd, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* provides passcode policy to show/check on client side */
CmsCore_API bool	CmsCore_UserAuth_getPolicy(EXP_LPCWSTR  pszCSN, /*authMethod*/DWORD preferedMeth, CAutoPtr<IAuthPolicy>& outPolicy, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* provides passcode policy to show/check on client side */
CmsCore_API bool	CmsCore_UserAuth_getCardDetails(EXP_LPCWSTR  pszCSN, IAuthDetails::detailType dwWhich, CAutoPtr<IAuthDetails>& outDetails, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	//
	//??????
	//
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* asks server for communication protocol version */
CmsCore_API bool	CmsCore_UserAuth_checkVersion(EXP_LPCWSTR  pszCSN, DWORD& dwInVersion, DWORD& dwOutVersion, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* reports card removal from client, so we can maintain current user card list */
CmsCore_API bool	CmsCore_UserAuth_cardRemoved(EXP_LPCWSTR  pszCSN, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}

/* add entry to transaction log */
CmsCore_API bool	CmsCore_UserAuth_addAuditRecord(EXP_LPCWSTR  pszCSN, int ActionID, EXP_LPCWSTR  pszParam1, CMSCORE_ERR* pRetErr, fkt_alloc p) {
	SET_OUT_ERROR(ERROR_CALL_NOT_IMPLEMENTED);
	return false;
}
#endif //(0)

/*
	ContainerCert
	Info: Name, Cert, ContainerId, KeySpec
	
	getCerts(CERTID*, DWORD* pCnt, IProgress*)
	getCertsName(CERTID
	issued to, issued by expiration date, PIN
	getCertsCert(CERTID
	getCertsIsDefault(CERTID

	lErr = pCard->SetDefaultContainer(m_CertInfo[l].getContainerIdx());
	lErr = pCard->DelContainer(m_CertInfo[lIdx].getContainerIdx(), lKeySpec);
	lErr = pCard->DelAllContainer();
	Import P12 / Cert
	DeleteAll

	SSO_get
	SSO_set

	TimedCache_get(enable, time)
	TimedCache_set(enable, time)

(	PINType
	writePP
	PP class to set/read SSO
)
CREATE_PIN_SET
GetPinString


Pin Policy class
Error handling
Drop down and card/pcsc handling
Card class ????
Read file for user info: Encoding !!!!
Buttons
WinDef.h : configure
Logging through Dll ???

*/
