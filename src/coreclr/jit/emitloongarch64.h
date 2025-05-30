// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_LOONGARCH64)

// The LOONGARCH64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.
//
typedef unsigned int code_t;

/************************************************************************/
/*         Routines that compute the size of / encode instructions      */
/************************************************************************/

struct CnsVal
{
    ssize_t cnsVal;
    bool    cnsReloc;
};

enum insDisasmFmt
{
    DF_G_INVALID = 0,
    DF_G_ALIAS   = 1, // alias instructions.
    DF_G_B2,
    DF_G_B1,
    DF_G_B0,
    // DF_G_2RK,
    DF_G_15I,
    DF_G_R20I,
    DF_G_2R,
    DF_G_2R5IU,
    DF_G_2R6IU,
    DF_G_2R5IW,
    DF_G_2R6ID,
    DF_G_2R12I,
    DF_G_2R12IU,
    DF_G_2R14I,
    DF_G_2R16I,
    DF_G_3R,
    DF_G_3R2IU,
    DF_G_3RX,
    // FPU
    DF_F_B1,
    DF_F_GR,
    DF_F_RG,
    DF_F_RG12I,
    DF_F_FG,
    DF_F_GF,
    DF_F_CR,
    DF_F_RC,
    DF_F_CG,
    DF_F_GC,
    DF_F_C2R,
    DF_F_2R,
    DF_F_R2G,
    DF_F_3R,
    DF_F_4R,
    DF_F_3RX3,
#ifdef FEATURE_SIMD
    // SIMD-128bits
    DF_S_4R,
    DF_S_3R,
    DF_S_RG,
    DF_S_2R,
    DF_S_2RX,
    DF_S_CR,
    DF_S_RGX,
    DF_S_GRX,
    DF_S_2RG,
    DF_S_2R3IU,
    DF_S_2R4IU,
    DF_S_2R5I,
    DF_S_2R5IU,
    DF_S_2R6IU,
    DF_S_2R7IU,
    DF_S_2R8IU,
    DF_S_2R8IX,
    DF_S_R13IU,
    // SIMD-256bits
    DF_A_4R,
    DF_A_3R,
    DF_A_RG,
    DF_A_2R,
    DF_A_CR,
    DF_A_RGX,
    DF_A_GRX,
    DF_A_2RG,
    DF_A_2RX,
    DF_A_2R3IU,
    DF_A_2R4IU,
    DF_A_2R5I,
    DF_A_2R5IU,
    DF_A_2R6IU,
    DF_A_2R7IU,
    DF_A_2R8IU,
    DF_A_R13IU,
    DF_A_RG8IX,
#endif
};

code_t       emitGetInsMask(int ins);
insDisasmFmt emitGetInsFmt(instruction ins);
void         emitDispInst(instruction ins);
void         emitDisInsName(code_t code, const BYTE* addr, instrDesc* id);

void emitIns_J_cond_la(instruction ins, BasicBlock* dst, regNumber reg1 = REG_R0, regNumber reg2 = REG_R0);
void emitIns_I_la(emitAttr attr, regNumber reg, ssize_t imm);

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(int              argCnt,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                               bool             hasAsyncRet);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                               bool             hasAsyncRet);

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsLoad(instruction ins);
bool emitInsIsStore(instruction ins);
bool emitInsIsLoadOrStore(instruction ins);

emitter::code_t emitInsCode(instruction ins /*, insFormat fmt*/);

// Generate code for a load or store operation and handle the case of contained GT_LEA op1 with [base + offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

//  Emit the 32-bit LOONGARCH64 instruction 'code' into the 'dst'  buffer
unsigned emitOutput_Instr(BYTE* dst, code_t code);

// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
static bool IsMovInstruction(instruction ins);

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
// Returns true if 'value' is a legal unsigned immediate 11 bit encoding.
static bool isValidUimm11(ssize_t value)
{
    return (0 == (value >> 11));
};

// Returns true if 'value' is a legal unsigned immediate 12 bit encoding.
static bool isValidUimm12(ssize_t value)
{
    return (0 == (value >> 12));
};

// Returns true if 'value' is a legal signed immediate 12 bit encoding.
static bool isValidSimm12(ssize_t value)
{
    return -(((int)1) << 11) <= value && value < (((int)1) << 11);
};

// Returns true if 'value' is a legal signed immediate 20 bit encoding.
static bool isValidSimm20(ssize_t value)
{
    return -(((int)1) << 19) <= value && value < (((int)1) << 19);
};

// Returns the number of bits used by the given 'size'.
inline static unsigned getBitWidth(emitAttr size)
{
    assert(size <= EA_8BYTE);
    return (unsigned)size * BITS_PER_BYTE;
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_INT_LAST);
}

inline static bool isGeneralRegisterOrR0(regNumber reg)
{
    return (reg >= REG_FIRST) && (reg <= REG_INT_LAST);
} // Includes REG_R0

inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_FP_FIRST && reg <= REG_FP_LAST);
}

#ifdef FEATURE_SIMD
inline static bool isVectorRegister(regNumber reg)
{
    return isFloatReg(reg);
}

//    For the given 'datasize', 'elemsize' and 'index' returns true, if it specifies a valid 'index'
//    for an element of size 'elemsize' in a vector register of size 'datasize'
static bool isValidVectorIndex(emitAttr datasize, emitAttr elemsize, ssize_t index);

inline static bool isValidVectorDatasize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_16BYTE) || (size == EA_32BYTE);
}

inline static bool isValidVectorElemsize(emitAttr size)
{
    return (size == EA_16BYTE) || (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE) || (size == EA_1BYTE);
}

void emitIns_S_R_SIMD12(regNumber ireg, int varx, int offs);
#endif

/************************************************************************/
/*                   Output target-independent instructions             */
/************************************************************************/

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);
void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);
void emitIns_I_I(instruction ins, emitAttr attr, ssize_t cc, ssize_t offs);

void emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
    emitIns_R_R(ins, attr, reg1, reg2);
}

void emitIns_R_R_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_I(instruction ins,
                     emitAttr    attr,
                     regNumber   reg1,
                     regNumber   reg2,
                     regNumber   reg3,
                     ssize_t     imm,
                     insOpts     opt      = INS_OPTS_NONE,
                     emitAttr    attrReg2 = EA_UNKNOWN);

void emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, regNumber reg4);

void emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber tmpReg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_AI(instruction  ins,
                  emitAttr     attr,
                  regNumber    reg,
                  ssize_t disp DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

unsigned emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* id, code_t code);

unsigned get_curTotalCodeSize(); // bytes of code

#endif // TARGET_LOONGARCH64
