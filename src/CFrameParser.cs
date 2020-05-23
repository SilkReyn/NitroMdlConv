using System;
using System.Collections.Generic;

using NitroMdlConv.Mdl;


namespace NitroMdlConv
{
    public class CFrameParser
    {
        public struct TagInfo
        {
            public enum Tags : int { frame=0, transform };
            public const string frame = "FRME";
            public const string transform = "TRNS";
        };


        readonly static IReadOnlyList<byte[]> mTags = new[] {
            new byte[] { 0x46, 0x52, 0x4d, 0x45 },  // FRME
            new byte[] { 0x54, 0x52, 0x4e, 0x53 },  // TRNS
        };  
        
        
        /// <summary>
        /// Takes bytes at current reader position and parses them, assuming it is a block with
        /// frame structure.
        /// Data is parsed even it has no frame structure, make sure the reader is after the
        /// corresponding node tag before this method call.
        /// </summary>
        /// <param name="frme">Contains parsed armature</param>
        /// <returns>Wether the result contains missing data, false if OK</returns>
        public static bool Parse(CMdlFileNavigator reader, out CFrame frme)
        {
            int numVal = 0;
            float[] buff = new float[16];
            bool corrupt = false;

            // Read terminated ascii c-string. Bones have a space prepend, rootnode has not.
            string name = reader.ReadText().TrimStart();
            if (String.IsNullOrEmpty(name))
            {
                name = "Unknown";
                corrupt = true;
            }

            reader.SeekBlockStart();  // Frame start, usually directly following
            if (reader.IsAtTag(mTags[(int)TagInfo.Tags.transform]))
            {
                reader.SeekBlockStart();
                for (; (numVal < buff.Length) && reader.HasMore(4); ++numVal)  // 4B float
                {
                    buff[numVal] = reader.ReadSingle();
                }
                reader.SeekBlockEnd();
            }
            if (numVal != buff.Length)
            {
                frme = new CFrame(name + "_broken");
                corrupt = true;
                return false;
            } else {
                frme = new CFrame(name, buff);
            }
            frme.IsDirty = corrupt;

            while (reader.IsAtTag(mTags[(int)TagInfo.Tags.frame]))
            {
                // Seek frame name. Frames have no block-open.
                reader.MoveCursor(TagInfo.frame.Length);

                corrupt |= Parse(reader, out CFrame child);
                frme.Childs.Add(child);
            }
            reader.SeekBlockEnd();
            return corrupt;
        }
    }
}
