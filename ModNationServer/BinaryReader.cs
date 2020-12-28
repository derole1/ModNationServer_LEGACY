using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModNationServer
{
    /*
     * BinaryReader by derole
     * 
     * A quick and dirty way to read binary data, works in a stack like fashion by pushing and poping data to and from the binary data
     * 
     * NOTE: This is currently designed to swap endinaness due to the game sending numerical values in big endian
     */

    class BinaryReader
    {
        int bReqIndex = 0;
        byte[] reqData;
        byte[] resData;
        bool endianFlip;

        public BinaryReader(byte[] request, bool flipEndian = true)
        {
            reqData = request;
            resData = new byte[0];
            endianFlip = flipEndian;
        }

        //Modifiers
        public void insertUInt32(uint data, int index)
        {
            byte[] toInsert = EndianFlip(BitConverter.GetBytes(data));
            for (int i = 0; i < toInsert.Length; i++)
            {
                resData[index + i] = toInsert[i];
            }
        }

        //GETs
        public byte[] getReq()
        {
            return reqData;
        }

        public byte[] getRes()
        {
            return resData;
        }

        public int getResLen()
        {
            return resData.Length;
        }

        public int getResIndex()
        {
            return bReqIndex;
        }

        //POPs
        public byte popByte()
        {
            var ret = reqData[bReqIndex];
            bReqIndex += 1;
            return ret;
        }

        public ushort popUInt16()
        {
            var ret = BitConverter.ToUInt16(EndianFlip(reqData, bReqIndex, 2), bReqIndex);
            bReqIndex += 2;
            return ret;
        }

        public short popInt16()
        {
            var ret = BitConverter.ToInt16(EndianFlip(reqData, bReqIndex, 2), bReqIndex);
            bReqIndex += 2;
            return ret;
        }

        public uint popUInt32()
        {
            var ret = BitConverter.ToUInt32(EndianFlip(reqData, bReqIndex, 4), bReqIndex);
            bReqIndex += 4;
            return ret;
        }

        public int popInt32()
        {
            var ret = BitConverter.ToInt32(EndianFlip(reqData, bReqIndex, 4), bReqIndex);
            bReqIndex += 4;
            return ret;
        }

        public ulong popUInt64()
        {
            var ret = BitConverter.ToUInt64(EndianFlip(reqData, bReqIndex, 8), bReqIndex);
            bReqIndex += 8;
            return ret;
        }

        public long popInt64()
        {
            var ret = BitConverter.ToInt64(EndianFlip(reqData, bReqIndex, 8), bReqIndex);
            bReqIndex += 8;
            return ret;
        }

        public float popFloat()
        {
            var ret = BitConverter.ToSingle(EndianFlip(reqData, bReqIndex, 4), bReqIndex);
            bReqIndex += 4;
            return ret;
        }

        public double popDouble()
        {
            var ret = BitConverter.ToDouble(EndianFlip(reqData, bReqIndex, 8), bReqIndex);
            bReqIndex += 8;
            return ret;
        }

        public bool popBool()
        {
            var ret = BitConverter.ToBoolean(reqData, bReqIndex);
            bReqIndex += 1;
            return ret;
        }

        public char popChar()
        {
            var ret = Convert.ToChar(reqData[bReqIndex]);
            bReqIndex += 1;
            return ret;
        }

        public char popUnicodeChar()
        {
            var ret = BitConverter.ToChar(reqData, bReqIndex);
            bReqIndex += 2;
            return ret;
        }

        public string popString()
        {
            string builtStr = "";
            char currentChar = char.MaxValue;
            while (currentChar != char.MinValue)
            {
                currentChar = Convert.ToChar(reqData[bReqIndex]);
                builtStr += currentChar;
                bReqIndex += 1;
            }
            return builtStr.Replace("\0", "");
        }

        public string popUnicodeString()
        {
            string builtStr = "";
            char currentChar = char.MaxValue;
            while (currentChar != char.MinValue)
            {
                currentChar = BitConverter.ToChar(reqData, bReqIndex);
                builtStr += currentChar;
                bReqIndex += 2;
            }
            return builtStr.Replace("\0", "");
        }

        public byte[] popArray(int length)
        {
            byte[] retArray = reqData.Skip(bReqIndex).Take(length).ToArray();
            bReqIndex += length;
            return retArray;
        }


        //PUSHs
        public void pushByte(byte data)
        {
            resData = resData.Concat(new byte[] { data }).ToArray();
        }

        public void pushUInt16(ushort data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushInt16(short data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushUInt32(uint data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushInt32(int data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushUInt64(ulong data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushInt64(long data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushFloat(float data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushDouble(double data)
        {
            resData = resData.Concat(EndianFlip(BitConverter.GetBytes(data))).ToArray();
        }

        public void pushBool(bool data)
        {
            resData = resData.Concat(BitConverter.GetBytes(data)).ToArray();
        }

        public void pushChar(char data, Encoding enc)
        {
            resData = resData.Concat(new byte[] { enc.GetBytes(data.ToString())[0] }).ToArray();
        }

        public void pushString(string data, Encoding enc)
        {
            resData = resData.Concat(enc.GetBytes(data + "\0")).ToArray();
        }

        public void pushArray(byte[] data)
        {
            resData = resData.Concat(data).ToArray();
        }

        //Helpers
        public byte[] EndianFlip(byte[] data, int offset = 0, int length = -1)
        {
            if (!endianFlip) { return data; }
            if (length < 0) { length = data.Length; }
            Array.Reverse(data, offset, length);
            return data;
        }
    }
}
