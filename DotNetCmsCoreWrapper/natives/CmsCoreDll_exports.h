#pragma once
#include "shared/Interfaces/IHttpSend.h"
#include "shared/Interfaces/IErrorStack.h"

#if defined(_MSC_VER)
    //  Microsoft
    typedef const wchar_t  *EXP_LPCWSTR;
    typedef wchar_t        *EXP_LPWSTR;
    typedef wchar_t         EXP_WCHAR;
    #define CmsCore_API __declspec(dllexport)
#elif defined(__GNUC__)
    #include <string>
    //#include <uchar.h>
    //  GCC
    #define CmsCore_API __attribute__((visibility("default")))
    #define IMPORT
    typedef char16_t        EXP_WCHAR;
    typedef const char16_t* EXP_LPCWSTR;
    typedef char16_t*       EXP_LPWSTR;
#else
    //  do nothing and hope for the best?
    #define EXPORT
    #define IMPORT
    #pragma warning Unknown dynamic link import/export semantics.
#endif

//General
typedef unsigned long       DWORD;
typedef unsigned int        UINT;
typedef unsigned char       BYTE;
typedef unsigned char       *LPBYTE;
#define NULL    0
#ifndef DECLARE_HANDLE
    #define DECLARE_HANDLE(name) struct name##__{int unused;}; typedef struct name##__ *name
    DECLARE_HANDLE            (HWND);
#endif

typedef void* (*fkt_alloc)(size_t);
typedef DWORD	HCMSCOREINST;
//typedef DWORD	CMSCORE_ERR;
typedef EXP_LPWSTR	CMSCORE_ERR;
typedef void*	ICmsCoreBasicPcsc;
typedef void*	ICmsCoreCard;
typedef void*	ICmsCoreCardList;
typedef DWORD	CONTAINER_ID;
//#define CmsCore_API __declspec(dllexport)

typedef struct CMSCORE_CARD_KEY_SIZES
{
	DWORD dwVersion;

	DWORD dwMinimumBitlen;
	DWORD dwDefaultBitlen;
	DWORD dwMaximumBitlen;
	DWORD dwIncrementalBitlen;

} CMSCORE_CARD_KEY_SIZES;


#ifndef __CARDMOD__H__	//cardmod.h

    #define AT_KEYEXCHANGE          1
    #define AT_SIGNATURE            2

	typedef     DWORD                       PIN_ID, *PPIN_ID;
	typedef     DWORD                       PIN_SET, *PPIN_SET;

	#define     MAX_PINS                    8

	#define     ROLE_EVERYONE               0
	#define     ROLE_USER                   1
	#define     ROLE_ADMIN                  2

	#define     PIN_SET_ALL_ROLES           0xFF
	#define     CREATE_PIN_SET(PinId)       (1 << PinId)
	#define     SET_PIN(PinSet, PinId)      PinSet |= CREATE_PIN_SET(PinId)
	#define     IS_PIN_SET(PinSet, PinId)   (0 != (PinSet & CREATE_PIN_SET(PinId)))
	#define     CLEAR_PIN(PinSet, PinId)    PinSet &= ~CREATE_PIN_SET(PinId)

	#define     PIN_CHANGE_FLAG_UNBLOCK     0x01
	#define     PIN_CHANGE_FLAG_CHANGEPIN   0x02

	#define     CP_CACHE_MODE_GLOBAL_CACHE  1
	#define     CP_CACHE_MODE_SESSION_ONLY  2
	#define     CP_CACHE_MODE_NO_CACHE      3

	#define     CARD_AUTHENTICATE_GENERATE_SESSION_PIN      0x10000000
	#define     CARD_AUTHENTICATE_SESSION_PIN               0x20000000

	#define     CARD_PIN_STRENGTH_PLAINTEXT                 0x1
	#define     CARD_PIN_STRENGTH_SESSION_PIN               0x2 

	#define     CARD_PIN_SILENT_CONTEXT                     0x00000040

	typedef enum
	{
		AlphaNumericPinType = 0,            // Regular PIN
		ExternalPinType,                    // Biometric PIN
		ChallengeResponsePinType,           // Challenge/Response PIN
		EmptyPinType                        // No PIN
	} SECRET_TYPE;

	typedef enum
	{
		AuthenticationPin,                  // Authentication PIN
		DigitalSignaturePin,                // Digital Signature PIN
		EncryptionPin,                      // Encryption PIN
		NonRepudiationPin,                  // Non Repudiation PIN
		AdministratorPin,                   // Administrator PIN
		PrimaryCardPin,                     // Primary Card PIN
		UnblockOnlyPin
	} SECRET_PURPOSE;

	typedef enum
	{
		PinCacheNormal = 0,
		PinCacheTimed,
		PinCacheNone,
		PinCacheAlwaysPrompt
	} PIN_CACHE_POLICY_TYPE;

	#define      PIN_CACHE_POLICY_CURRENT_VERSION     6

	typedef struct _PIN_CACHE_POLICY
	{
        DWORD                                 dwVersion;
        PIN_CACHE_POLICY_TYPE                 PinCachePolicyType;
        DWORD                                 dwPinCachePolicyInfo;
	} PIN_CACHE_POLICY, *PPIN_CACHE_POLICY;

	#define      PIN_INFO_CURRENT_VERSION             6

	#define      PIN_INFO_REQUIRE_SECURE_ENTRY        1

	typedef struct _PIN_INFO
	{
        DWORD                                 dwVersion;
        SECRET_TYPE                           PinType;
        SECRET_PURPOSE                        PinPurpose;
        PIN_SET                               dwChangePermission;
        PIN_SET                               dwUnblockPermission;
        PIN_CACHE_POLICY                      PinCachePolicy;
        DWORD                                 dwFlags;
	} PIN_INFO, *PPIN_INFO;

	// Logical Directory Access Conditions
	typedef enum
	{
		InvalidDirAc = 0,

		// User Read, Write
		UserCreateDeleteDirAc,

		// Admin Write
		AdminCreateDeleteDirAc

	} CARD_DIRECTORY_ACCESS_CONDITION;

	// Logical File Access Conditions
	typedef enum
	{
		// Invalid value, chosed to cooincide with common initialization
		// of memory
		InvalidAc = 0,

		// Everyone     Read
		// User         Read, Write
		//
		// Example:  A user certificate file.
		EveryoneReadUserWriteAc,

		// Everyone     None
		// User         Write, Execute
		//
		// Example:  A private key file.
		UserWriteExecuteAc,

		// Everyone     Read
		// Admin        Read, Write
		//
		// Example:  The Card Identifier file.
		EveryoneReadAdminWriteAc,

		// Explicit value to set when it is desired to say that
		// it is unknown
		UnknownAc,

		// Everyone No Access 
		// User Read Write 
		// 
		// Example:  A password wallet file. 

		UserReadWriteAc,
		// Everyone/User No Access 
		// Admin Read Write 
		// 
		// Example:  Administration data. 

		AdminReadWriteAc
	} CARD_FILE_ACCESS_CONDITION;


#endif
extern "C" {
CmsCore_API bool	CmsCore_Initialize(HCMSCOREINST* pOutInst, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_Finalize(HCMSCOREINST hInst);
CmsCore_API bool	CmsCore_GetErrorDetails(DWORD err, EXP_LPWSTR* pOutErrStr, fkt_alloc p);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Readers/Cards/List
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class ICmsCoreCardStatusChangeNotify {
public:
	virtual void OnCardInsert(ICmsCoreBasicPcsc* pPcsc) = 0;
	virtual void OnCardRemove(ICmsCoreBasicPcsc* pPcsc) = 0;
};


class ICmsCoreProgress {
    public:
        virtual ~ICmsCoreProgress()  {}
        virtual void Show(int iWhat) = 0;
        virtual void SetRange(int iStart, int iEnd) = 0;
        virtual void SetPos(int i) = 0;
        virtual int  SetStep(int i) = 0;
        virtual void StepIt() = 0;
        virtual void OnStart() = 0;
        virtual void OnEnd() = 0;
        virtual void SetMsg(EXP_LPCWSTR pMsg, int idx=0) = 0;
        virtual void Progress(EXP_LPCWSTR pszMsg=NULL, int idx=0) = 0;
        virtual void SetRemainingTime(EXP_LPCWSTR pMsg) {}
        virtual void WaitCursor(bool bOn) {}
        virtual DWORD StatusTakeSnapshot() { return 0; }
        virtual void  StatusRevertToSnapshot(DWORD dwID) {}
    };

  class ICmsCoreReaderList {
    public:
        virtual UINT            getCnt() = 0;
        virtual EXP_LPCWSTR     get(UINT idx) = 0;
        virtual void            add(EXP_LPCWSTR pReaderName) = 0;
        virtual void            del(UINT idx) = 0;
        virtual int             find(EXP_LPCWSTR pReaderName) = 0;
        virtual void            ResetContent() = 0;
        virtual	int             GetCurSel() = 0;
        virtual void            SetCurSel(int idx) = 0;

        bool		bLastReaderAvail;
        bool		bReaderSelected;
        int			lastReaderCnt;
        DWORD		dwLastUpdate;
        DWORD		dwLastReaderWrite;
        bool		bRdUpdateRequired;
        EXP_WCHAR		szForceSelectedReader[512];
    };
extern "C" {
CmsCore_API bool	CmsCore_GetConfigParam					(EXP_LPCWSTR pszCfgFile, EXP_LPCWSTR pszKey, EXP_LPWSTR* pOutValue, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_GetLicenseParam					(EXP_LPCWSTR pszLicFile, EXP_LPCWSTR pszPubKey, EXP_LPCWSTR pszKey, EXP_LPWSTR* pOutValue, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CardList_Get					(HCMSCOREINST hInst, ICmsCoreCardList* pOuthCardList, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_UpdateReaderList		(ICmsCoreCardList hCardList, ICmsCoreReaderList* pReaderList, bool bCardPrinterOnly, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_GetAvailableCardsCnt	(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderToSkip, UINT* outCnt, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_GetCardPcsc			(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderName, ICmsCoreBasicPcsc* pOutPcsc, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_clearCaches			(ICmsCoreCardList hCardList, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_GetCardReaderName		(ICmsCoreCardList hCardList, UINT Idx, EXP_LPCWSTR pszReaderToSkip, EXP_LPWSTR* pOutReaderName, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CardList_isCardAvailable		(ICmsCoreCardList hCardList, EXP_LPCWSTR pszReaderName, bool* pOutAvail, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_isValid				(ICmsCoreCardList hCardList, ICmsCoreBasicPcsc hPcsc, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_AddCardStatusNotifier	(ICmsCoreCardList hCardList, ICmsCoreCardStatusChangeNotify*pNotify, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CardList_DelCardStatusNotifier	(ICmsCoreCardList hCardList, ICmsCoreCardStatusChangeNotify*pNotify, CMSCORE_ERR* pRetErr, fkt_alloc p);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//PCSC
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Cards General
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_CardSupported					(HCMSCOREINST hInst, LPBYTE atr, size_t atrSize, DWORD* pOutCardType, EXP_LPWSTR* pOutCardName, fkt_alloc p, CMSCORE_ERR* pRetErr);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Card Object
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
CmsCore_API bool	CmsCore_Pcsc_GetAtr						(ICmsCoreBasicPcsc hPcsc, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_Create_CmsCard					(ICmsCoreBasicPcsc hPcsc, ICmsCoreCard* pOuthCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_Delete_CmsCard					(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);

CmsCore_API bool	CmsCore_CmsCard_identifyCard			(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_lockCard				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_unlockCard				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_emptyCache				(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_get_CSN_CARDID			(ICmsCoreCard hCard, EXP_LPWSTR* pOutCSN, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_get_CSN_CSN				(ICmsCoreCard hCard, EXP_LPWSTR* pOutCSN, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_getChallenge			(ICmsCoreCard hCard, LPBYTE* pOutCryptogram, DWORD* pOutCryptogramSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_unblockUserPin			(ICmsCoreCard hCard, LPBYTE cryptogram, size_t cryptogramSize, EXP_LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin			(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR puc, EXP_LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_getRoleTriesLeft		(ICmsCoreCard hCard, PIN_ID bRole, DWORD* pOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_getAdminTriesLeft		(ICmsCoreCard hCard, DWORD* pOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_changeRolePin			(ICmsCoreCard hCard,  PIN_ID bRole, EXP_LPCWSTR oldPin, EXP_LPCWSTR newPin, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_loginRole				(ICmsCoreCard hCard,  PIN_ID bRole, EXP_LPCWSTR pin, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_roleAuthenticated		(ICmsCoreCard hCard, PIN_ID bRole, bool* pOutAuthenticated, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_loginAdmin				(ICmsCoreCard hCard,  LPBYTE cryptogram, size_t cryptogramSize, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_setAdminKey				(ICmsCoreCard hCard,  LPBYTE cryptogram, size_t cryptogramSize, LPBYTE newKey, size_t newKeySize, int iNewTries, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_blockRolePin			(ICmsCoreCard hCard,  PIN_ID bRole, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_correctFileName			(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, EXP_LPWSTR* pOutName, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_readFile				(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_writeFile				(ICmsCoreCard hCard,  EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, LPBYTE pInData, size_t inDataSize, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_createFile				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, long dwInitialSize, CARD_FILE_ACCESS_CONDITION ac, bool bFailIfExists, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_deleteFile				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPCWSTR pszFileName, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_createDirectory			(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, bool bAdminOnly, bool bFailIfExists, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_deleteDirectory			(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_listFiles				(ICmsCoreCard hCard, EXP_LPCWSTR pszDirName, EXP_LPWSTR* pOutStr, DWORD* dwOutCnt, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_createContainer			(ICmsCoreCard hCard, BYTE bIdx, bool bKeyImport, long keySpec, long dwKeySize, LPBYTE pInKey, size_t inKeySize, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_deleteContainer			(ICmsCoreCard hCard, BYTE bIdx, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_rsaDecrypt				(ICmsCoreCard hCard, BYTE bIdx, int iKeySpec, LPBYTE pInData, size_t inDataSize, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);

CmsCore_API bool	CmsCore_CmsCard_readPinPolicy1			(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_readPinPolicy2			(ICmsCoreCard hCard, PIN_ID bRole, bool bReadTriesCounter, LPBYTE* pOutData, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_writePinPolicy			(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE pData, DWORD dwDataSize, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_readCardProperty		(ICmsCoreCard hCard, PIN_ID bRole, DWORD dwProp, DWORD& dwOutVal, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_writeCardProperty		(ICmsCoreCard hCard, PIN_ID bRole, DWORD dwProp, DWORD& dwVal, CMSCORE_ERR* pRetErr, fkt_alloc p);
//CmsCore_API bool	CmsCore_CmsCard_PinPolicyCheckPin		(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pin, bool bConfirmed, EXP_LPWSTR* pOutResStr, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_getPinName				(ICmsCoreCard hCard, PIN_ID bRole, PIN_SET unblockPinList, EXP_LPWSTR* pOutName, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_getKeySizes				(ICmsCoreCard hCard, DWORD dwKeySpec, CMSCORE_CARD_KEY_SIZES* pOutKeySizes, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_getFreeSpaces			(ICmsCoreCard hCard, DWORD& dwBytesAvaiable, DWORD& dwKeyContainerAvailable, DWORD& dwMaxKeyContainers, CMSCORE_ERR* pRetErr, fkt_alloc p);
//public void     CmsCore_CmsCard_setAdminKeyIsDefault(byte[] cur_key, bool bIsDefault) throws Exception;
CmsCore_API bool	CmsCore_CmsCard_getCardContainerPinList	(ICmsCoreCard hCard, bool bAddPrimaryUserPin, PIN_SET* pOut, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_getUnblockPinList		(ICmsCoreCard hCard, PIN_SET pinList, PIN_SET* pOut, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_PinPolicyCheckPin		(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pin, bool bConfirmed, bool bIncludeConfirm, bool* pOutResult, EXP_LPWSTR* pOutResStr, fkt_alloc p, CMSCORE_ERR* pRetErr);

CmsCore_API bool	CmsCore_CmsCard_SSO_Set					(ICmsCoreCard hCard, PIN_ID bRole, bool bEnabled, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_SSO_Get					(ICmsCoreCard hCard, PIN_ID bRole, bool* bOutEnabled, CMSCORE_ERR* pRetErr, fkt_alloc p);

CmsCore_API bool	CmsCore_CmsCard_ContainersGet			(ICmsCoreCard hCard, CONTAINER_ID** pOutIds, DWORD* pOutSize, fkt_alloc p, ICmsCoreProgress* pProgress, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_ContainersGetLabel		(ICmsCoreCard hCard, CONTAINER_ID contId, int iWhich, EXP_LPWSTR* pOutResStr, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_ContainersGetCert		(ICmsCoreCard hCard, CONTAINER_ID contId, LPBYTE* pOutCert, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_ContainersGetIsDefault	(ICmsCoreCard hCard, CONTAINER_ID contId, bool* pOutIsDefault, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_ContainersShowCert		(ICmsCoreCard hCard, CONTAINER_ID contId, HWND hParentWnd, EXP_LPCWSTR pszTitle, bool bEnableImport, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_ContainersSetIsDefault	(ICmsCoreCard hCard, CONTAINER_ID contId, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_ContainersDelete		(ICmsCoreCard hCard, CONTAINER_ID contId, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_ContainersDeleteAll		(ICmsCoreCard hCard, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_ContainersImportCert	(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pszRolePin, EXP_LPCWSTR pszFilename, EXP_LPCWSTR pszPin, CONTAINER_ID contId, int iKeySpec, EXP_LPWSTR* pOutErrStr, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CmsCard_ContainersImportCerts	(ICmsCoreCard hCard, PIN_ID bRole, EXP_LPCWSTR pszRolePin, LPBYTE pbCertData, DWORD dwCertDataSize, DWORD& dwOutAddedCnt, CMSCORE_ERR* pRetErr, fkt_alloc p);

CmsCore_API bool	CmsCore_CmsCard_readPinInfo				(ICmsCoreCard hCard, PIN_ID bRole, PIN_INFO* pOutData, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_writePinInfo			(ICmsCoreCard hCard, PIN_ID bRole, PIN_INFO* pOutData, CMSCORE_ERR* pRetErr, fkt_alloc p);

CmsCore_API bool	CmsCore_CheckForUpdates					(EXP_LPCWSTR pszUrl, EXP_LPCWSTR pszXmlNode, bool& bOutUpdateAvailable, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_CheckForUpdates1				(IHttpSend* pHttpSend, EXP_LPCWSTR pszUrl, EXP_LPCWSTR pszXmlNode, bool& bOutUpdateAvailable, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr);

CmsCore_API bool	CmsCore_GenLicenseUpgradeChallenge		( BYTE bWhich, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_ApplyLicenseUpgradeResponse		(EXP_LPCWSTR pszResp, bool bInstall, bool& bOk, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr);

CmsCore_API bool	CmsCore_P12Parse						(EXP_LPCWSTR pszP12File, EXP_LPCWSTR pszPwd, DWORD& dwOutInst, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_P12GetCnt						(DWORD	dwInst, DWORD& dwOutCnt, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_P12GetLabel						(DWORD	dwInst, DWORD dwIdx, EXP_LPWSTR* pOutMsgStr, fkt_alloc p, CMSCORE_ERR* pRetErr);
CmsCore_API bool	CmsCore_P12MarkToDelete					(DWORD	dwInst, DWORD dwIdx, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_P12GetCert						(DWORD	dwInst, LPBYTE* pOutCerts, DWORD* pOutDataSize, fkt_alloc p, CMSCORE_ERR* pRetErr);

//New
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin1(ICmsCoreCard hCard, PIN_ID bRole, LPBYTE cryptogram, size_t cryptogramSize, LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool	CmsCore_CmsCard_unblockRolePin2(ICmsCoreCard hCard, PIN_ID bRole, PIN_ID bPukRole, LPCWSTR pszPuk,          LPCWSTR newPin, int iTriesLeft, CMSCORE_ERR* pRetErr, fkt_alloc p);

typedef struct T_LICCHECK {
	LPBYTE	pInData;
	DWORD	dwInDataSize;
	LPBYTE	pOutData;
	DWORD	dwOutDataSize;
} T_LICCHECK;

CmsCore_API bool	CmsCore_CheckLicenseUpgradeChallenge		(T_LICCHECK *pStruct, BYTE bVal, fkt_alloc p, CMSCORE_ERR* pRetErr);


//Localization specific
CmsCore_API bool		CmsCore_Init_Localization(HCMSCOREINST hInst, EXP_LPWSTR languageId, EXP_LPCWSTR pszLocFileName, DWORD dwFlags, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool		CmsCore_End_Localization(HCMSCOREINST hInst, CMSCORE_ERR* pRetErr, fkt_alloc p);
CmsCore_API bool		CmsCore_Translate(EXP_LPCWSTR pStringToTranslate, EXP_LPCWSTR trType, EXP_LPCWSTR pszSrcFile, int iLine, EXP_LPWSTR* pOutTranslatedString, fkt_alloc p, CMSCORE_ERR* pRetErr);

} //extern "C"
