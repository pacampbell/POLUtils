// Copyright © 2010-2012 Chris Baggett, Tim Van Holder, Nevin Stepan
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS"
// BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using System.Collections.Generic;
using PlayOnline.Core;
/**
 * Older file format from ~2010
 * 32-byte overall file header
 * 16 byte header before the spell section
 * 768 blocks of 64 bytes each for spells
 * 16 byte header before the abilities section
 * 1024 blocks of 48 bytes each for abilities
 * 16 byte footer at the end of the file

 * For the spells, the data structure is:
 * Index (2 bytes)
 * Magic Type (2 bytes)
 * Element (2 bytes)
 * Valid Targets (2 bytes)
 * Skill (2 bytes)
 * MP Cost (2 bytes)
 * Cast Time (1 byte)
 * Recast Time (1 byte)
 * Job levels (24 bytes, each byte representing a job in the order: ??? ??? ??? WHM BLM RDM ??? PLD DRK ??? BRD ??? ??? NIN ??? SMN BLU ??? ??? ??? SCH ??? ??? ???)
 * ID (2 bytes)
 * Unknown (2 bytes)

 * For the abilities, the data structure is:
 * ID (2 bytes)
 * Type (1 byte)
 * List Icon ID (1 byte)
 * MP Cost (2 bytes)
 * Unknown (2 bytes)
 * Valid Targets (2 bytes)
 */
namespace PlayOnline.FFXI.FileTypes
{
    public class SpellAndAbilityInfo : FileType
    {
        public override ThingList Load(BinaryReader BR, ProgressCallback ProgressCallback)
        {
            ThingList TL = new ThingList();
            if (ProgressCallback != null) {
                ProgressCallback(I18N.GetText("FTM:CheckingFile"), 0);
            }
            // Do some sanity checks
            if (BR.BaseStream.Position != 0) {
                goto Failed;
            }
            // Start reading the header bytes, 32 bytes in total 10/20/2016
            // First check for the idetifying byte sequence?
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "menu") {
                goto Failed;
            }
            // Read in a magic number ?
            if (BR.ReadInt32() != 0x101) {
                goto Failed;
            }
            // Consume pad bytes
            for (int i = 0; i < 3; i++) {
                if (BR.ReadInt64() != 0) {
                    goto Failed;
                }
            }
            // Update the progress
            if (ProgressCallback != null) {
                ProgressCallback(I18N.GetText("FTM:LoadingData"), (double)BR.BaseStream.Position / BR.BaseStream.Length);
            }

            // There seems to exist four sections now
            // 1. 0x80 mon_
            // 2. 0x80 levc (skip for now)
            // 3. 0x80 mgc_
            // 4. 0x30 comm
            
            // Get the first section mon_
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "mon_") {
                goto Failed;
            }
            // Eat up some padding?
            uint SizeInfo = BR.ReadUInt32();
            if (BR.ReadUInt64() != 0) {
                goto Failed;
            }
            // Make sure the calculated block size is correct (should only matter after game update)
            uint BlockSize = (SizeInfo & 0xFFFFFF80) >> 3;
            if ((BlockSize - 0x10) % 0x80 != 0) {
                goto Failed;
            }
            uint EntryCount = (BlockSize - 0x10) / 0x80;
            while (EntryCount-- > 0) {
                Things.MonsterSpellInfo2 MSI2 = new Things.MonsterSpellInfo2();
                if (!MSI2.Read(BR)) {
                    goto Failed;
                }
                if (ProgressCallback != null) {
                    ProgressCallback(null, (double)BR.BaseStream.Position / BR.BaseStream.Length);
                }
                TL.Add(MSI2);
            }
            
            // Get the second section levc and just move past it for now
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "levc") {
                goto Failed;
            }
            // Eat up some padding?
            SizeInfo = BR.ReadUInt32();
            if (BR.ReadUInt64() != 0) {
                goto Failed;
            }
            // Make sure the calculated block size is correct (should only matter after game update)
            BlockSize = (SizeInfo & 0xFFFFFF80) >> 3;
            if ((BlockSize - 0x10) % 0x80 != 0) {
                goto Failed;
            }
            BR.BaseStream.Position += (BlockSize - 0x10);
            
            // Get the third section mgc_ 
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "mgc_") {
                goto Failed;
            }
            // Eat up some padding?
            SizeInfo = BR.ReadUInt32();
            if (BR.ReadUInt64() != 0) {
                goto Failed;
            }
            // Make sure the calculated block size is correct (should only matter after game update)
            BlockSize = (SizeInfo & 0xFFFFFF80) >> 3;
            if ((BlockSize - 0x10) % 0x80 != 0) {
                goto Failed;
            }
            EntryCount = (BlockSize - 0x10) / 0x80;
            while (EntryCount-- > 0) {
                Things.SpellInfo2 SI2 = new Things.SpellInfo2();
                if (!SI2.Read(BR)) {
                    goto Failed;
                }
                if (ProgressCallback != null) {
                    ProgressCallback(null, (double)BR.BaseStream.Position / BR.BaseStream.Length);
                }
                TL.Add(SI2);
            }

            // Get the fourth section comm
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "comm") {
                goto Failed;
            }
            // Eat up some padding?
            SizeInfo = BR.ReadUInt32();
            if (BR.ReadUInt64() != 0) {
                goto Failed;
            }
            // Make sure the calculated block size is correct (should only matter after game update)
            BlockSize = (SizeInfo & 0xFFFFFF80) >> 3;
            if ((BlockSize - 0x10) % 0x30 != 0) {
                goto Failed;
            }
            EntryCount = (BlockSize - 0x10) / 0x30;
            while (EntryCount-- > 0) {
                Things.AbilityInfo2 AI2 = new Things.AbilityInfo2();
                if (!AI2.Read(BR)) {
                    goto Failed;
                }
                if (ProgressCallback != null) {
                    ProgressCallback(null, (double)BR.BaseStream.Position / BR.BaseStream.Length);
                }
                TL.Add(AI2);
            }

            // Make sure we reached the end
            if (Encoding.ASCII.GetString(BR.ReadBytes(4)) != "end\0") {
                goto Failed;
            }
            // Eat up some padding?
            SizeInfo = BR.ReadUInt32();
            if (BR.ReadUInt64() != 0) {
                goto Failed;
            }
            // Make sure the calculated block size is correct (should only matter after game update)
            BlockSize = (SizeInfo & 0xFFFFFF80) >> 3;
            if (BlockSize != 0x10) {
                goto Failed;
            }

            // Update the progress callback
            if (ProgressCallback != null) {
                ProgressCallback(null, (double)BR.BaseStream.Position / BR.BaseStream.Length);
            }


            // We reached the end! Skip the failed label
            goto Done;
        Failed:
            TL.Clear();
        Done:
            return TL;
        }
    }
}
