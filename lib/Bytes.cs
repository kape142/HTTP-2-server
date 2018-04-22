using System;
using System.Collections.Generic;
using System.Text;

namespace lib
{
    public static class Bytes
    {
        public static byte[] CombineByteArrays(params byte[][] arrays)
        {
            int size = 0;
            foreach (byte[] b in arrays)
            {
                size += b.Length;
            }

            byte[] array = new byte[size];

            int i = 0;
            foreach (byte[] bA in arrays)
                foreach (byte b in bA)
                    array[i++] = b;
            return array;
        }

        public static int ConvertFromIncompleteByteArray(byte[] array)
        {
            byte[] target = new byte[4];
            for (int i = 0; i < array.Length; i++)
                target[i + (4 - array.Length)] = array[i];
            return (target[0] << 24) + (target[1] << 16) + (target[2] << 8) + target[3];
        }

        public static byte[] ConvertToByteArray(int number, int bytes = 4)
        {
            return ConvertToByteArray((long)number, bytes);
        }

        public static byte[] ConvertToByteArray(long number, int bytes = 4)
        {
            if (bytes > 8) throw new Exception("too many bytes requested");
            var byteArr = new byte[bytes];
            var numArr = ExtractBytes(number);
            for (int j = 0; j < bytes; j++)
                byteArr[j] = numArr[j + (8 - bytes)];
            return byteArr;
        }

        public static byte[] ExtractBytes(long num)
        {
            byte[] b = new byte[8];
            for (int i = 0; i < 8; i++)
                b[i] = (byte)(num >> (56 - i * 8));
            return b;
        }

        public static byte[] ExtractBytes(int num)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
                b[i] = (byte)(num >> (24 - i * 8));
            return b;
        }

        public static byte[] ExtractBytes(short num)
        {
            byte[] b = new byte[2];
            for (int i = 0; i < 2; i++)
                b[i] = (byte)(num >> (8 - i * 8));
            return b;
        }

        public static byte[] GetPartOfByteArray(int start, int end, byte[] b)
        {
            byte[] part = new byte[end - start];
            for (int i = 0; i < end - start; i++)
            {
                part[i] = b[start + i];
            }
            return part;
        }

        public static (bool bit32, int int31) Split32BitToBoolAnd31bitInt(int i)
        {
            int _bit32 = (int)(i & 0b10000000000000000000000000000000);
            bool _bit = (_bit32 == -2147483648) ? true : false;
            int _uint32 = (int)(i & 0b01111111111111111111111111111111);
            return (_bit, _uint32);
        }

        public static int PutBoolAndIntTo32bitInt(bool bit32, int int31)
        {
            return bit32 ? (int)(int31 | 0x80000000) : (int)(int31 & 0x7fffffff);
        }
    }
}
