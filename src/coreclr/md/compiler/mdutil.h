// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDUtil.h
//

//
// Contains utility code for MD directory
//
//*****************************************************************************
#ifndef __MDUtil__h__
#define __MDUtil__h__

#include "metadata.h"


HRESULT _GetFixedSigOfVarArg(           // S_OK or error.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob of CLR method signature
    ULONG   cbSigBlob,                  // [IN] size of signature
    CQuickBytes *pqbSig,                // [OUT] output buffer for fixed part of VarArg Signature
    ULONG   *pcbSigBlob);               // [OUT] number of bytes written to the above output buffer

ULONG _GetSizeOfConstantBlob(
    DWORD       dwCPlusTypeFlag,            // ELEMENT_TYPE_*
    void        *pValue,                    // BLOB value
    ULONG       cchString);                 // Size of string in wide chars, or -1 for auto.

#if defined(FEATURE_METADATA_IN_VM)

class RegMeta;

//*********************************************************************
//
// Structure to record the all loaded modules and helpers.
// RegMeta instance is added to the global variable that is tracking
// the opened scoped. This happens in RegMeta's constructor.
// In RegMeta's destructor, the RegMeta pointer will be removed from
// this list.
//
//*********************************************************************
class UTSemReadWrite;

class LOADEDMODULES : public CDynArray<RegMeta *>
{
private:
    static HRESULT InitializeStatics();

    // Global per-process list of loaded modules
    static LOADEDMODULES * s_pLoadedModules;

public:
    // Named for locking macros - see code:LOCKREAD
    static UTSemReadWrite * m_pSemReadWrite;

    static HRESULT AddModuleToLoadedList(RegMeta *pRegMeta);
    static BOOL RemoveModuleFromLoadedList(RegMeta *pRegMeta);  // true if found and removed.

    static HRESULT ResolveTypeRefWithLoadedModules(
        mdTypeRef          tkTypeRef,       // [IN] TypeRef to be resolved.
        RegMeta *          pTypeRefRegMeta, // [IN] Scope in which the TypeRef is defined.
        IMetaModelCommon * pTypeRefScope,   // [IN] Scope in which the TypeRef is defined.
        REFIID             riid,            // [IN] iid for the return interface.
        IUnknown **        ppIScope,        // [OUT] Return interface.
        mdTypeDef *        ptd);            // [OUT] TypeDef corresponding the TypeRef.

#ifdef _DEBUG
    static BOOL IsEntryInList(RegMeta *pRegMeta);
#endif
};  // class LOADEDMODULES

#endif //FEATURE_METADATA_IN_VM

#endif // __MDUtil__h__
