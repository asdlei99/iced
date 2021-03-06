/*
Copyright (C) 2018-2019 de4dot@gmail.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Iced.Intel;

namespace IcedFuzzer.Core {
	readonly struct RegisterInfo {
		readonly int bitness;
		readonly FuzzerOperandRegLocation regLoc;
		public readonly uint MaxRegCount;

		public RegisterInfo(int bitness, FuzzerOperandRegLocation regLoc, uint maxRegCount) {
			this.bitness = bitness;
			this.regLoc = regLoc;
			MaxRegCount = maxRegCount;
		}

		public uint MaskOutIgnoredBits(FuzzerRegisterKind register, uint regNum) {
			Assert.True(regNum < MaxRegCount);

			// We return reg count == 256. The other bits are ignored or used as an immediate
			if (regLoc == FuzzerOperandRegLocation.Is4Bits || regLoc == FuzzerOperandRegLocation.Is5Bits)
				regNum &= 0xF;

			if (bitness < 64 && regLoc == FuzzerOperandRegLocation.VvvvBits)
				regNum &= 7;

			switch (register) {
			case FuzzerRegisterKind.K:
				if (regLoc == FuzzerOperandRegLocation.ModrmRmBits)
					return regNum & 7;
				break;

			case FuzzerRegisterKind.Segment:
			case FuzzerRegisterKind.ST:
			case FuzzerRegisterKind.MM:
				return regNum & 7;

			case FuzzerRegisterKind.GPR8:
			case FuzzerRegisterKind.GPR16:
			case FuzzerRegisterKind.GPR32:
			case FuzzerRegisterKind.GPR64:
				if (bitness < 64)
					return regNum & 7;
				if (regLoc == FuzzerOperandRegLocation.ModrmRegBits)
					return regNum;
				return regNum & 15;

			case FuzzerRegisterKind.CR:
			case FuzzerRegisterKind.DR:
			case FuzzerRegisterKind.TR:
			case FuzzerRegisterKind.BND:
			case FuzzerRegisterKind.TMM:
				break;

			case FuzzerRegisterKind.XMM:
			case FuzzerRegisterKind.YMM:
			case FuzzerRegisterKind.ZMM:
				if (bitness < 64)
					return regNum & 7;
				break;

			default:
				throw ThrowHelpers.Unreachable;
			}

			return regNum;
		}

		// Checks if the reg is valid, ignoring any ignored bits in regNum
		public bool IsValid(FuzzerInstruction instruction, FuzzerRegisterKind register, uint regNum) {
			Assert.True(regNum < MaxRegCount);
			regNum = MaskOutIgnoredBits(register, regNum);

			switch (register) {
			case FuzzerRegisterKind.Segment:
				switch (instruction.Code) {
				case Code.Mov_Sreg_rm16:
				case Code.Mov_Sreg_r32m16:
				case Code.Mov_Sreg_r64m16:
					// Can't move to CS (except if it's an 8086/8088)
					if (regNum == 1)
						return false;
					break;
				}
				// ES,CS,SS,DS,FS,GS
				return regNum < 6;

			case FuzzerRegisterKind.CR:
				// CR{0,2,3,4,8}
				return regNum != 1 && (regNum == 8 || regNum < 5);

			case FuzzerRegisterKind.DR:
				// DR{0,1,2,3,4,5,6,7}
				return regNum < 8;

			case FuzzerRegisterKind.K:
				if (regLoc == FuzzerOperandRegLocation.AaaBits && regNum == 0 && instruction.RequireNonZeroOpMaskRegister)
					return false;
				break;

			case FuzzerRegisterKind.GPR8:
			case FuzzerRegisterKind.GPR16:
			case FuzzerRegisterKind.GPR32:
			case FuzzerRegisterKind.GPR64:
			case FuzzerRegisterKind.ST:
			case FuzzerRegisterKind.TR:
			case FuzzerRegisterKind.BND:
			case FuzzerRegisterKind.MM:
			case FuzzerRegisterKind.XMM:
			case FuzzerRegisterKind.YMM:
			case FuzzerRegisterKind.ZMM:
			case FuzzerRegisterKind.TMM:
				break;

			default:
				throw ThrowHelpers.Unreachable;
			}

			return regNum < RegisterUtils.GetRegisterCount(register);
		}
	}
}
