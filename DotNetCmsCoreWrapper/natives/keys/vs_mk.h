#pragma once

class CVS_MK {
public:
	CVS_MK();
	~CVS_MK();
	CVS_MK&	operator=(CVS_MK&b);
	BYTE*	GetData()	{ return m_Data.GetData(); }
	INT_PTR	GetSize()	{ return m_Data.GetSize(); }
	CMArray<BYTE, BYTE>&	GetKey()	{ return m_Data; }
	BYTE*	GetKey1()	{ return &m_Data[0]; }
	BYTE*	GetKey2()	{ return &m_Data[8]; }
	BYTE*	GetKey3()	{ return &m_Data[16]; }
private:
	CMArray<BYTE, BYTE>	m_Data;
};


inline CVS_MK::CVS_MK() {
	CMString	Str = 
	#include "vs_mk.inc"
	int  len=Str.GetLength();
	for (int i = 0; i < len; i++) Str.SetAt(i, (char)(255 - (BYTE)(Str[i])));
	ConvertHexStringToByteArray(Str, m_Data);
	LPTSTR p = Str.GetBuffer();
	memset(p, 0x00, Str.GetLength()*sizeof(TCHAR));
	Str.ReleaseBuffer();
}

inline CVS_MK::~CVS_MK() {
	//flush Memory
	if(m_Data.GetSize())
		memset(m_Data.GetData(), 0x00, m_Data.GetSize());
}

inline CVS_MK&	CVS_MK::operator=(CVS_MK&b) {
	m_Data.SetSize(b.m_Data.GetSize());
	memcpy(m_Data.GetData(), b.m_Data.GetData(), m_Data.GetSize());
	return *this;
}
/////////////////////////////////////////////////////////////////////////////////////

