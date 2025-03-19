using System;
using System.IO;
using System.Text;

public class CRC32
{
    private static readonly uint[] Crc32Table;

    static CRC32()
    {
        const uint polynomial = 0xedb88320;
        Crc32Table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (uint j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            Crc32Table[i] = crc;
        }
    }

    public static uint CalculateCrc32(byte[] bytes)
    {
        uint crc = 0xffffffff;
        foreach (byte b in bytes)
        {
            byte tableIndex = (byte)(((crc) & 0xff) ^ b);
            crc = (crc >> 8) ^ Crc32Table[tableIndex];
        }
        return ~crc;
    }

    public static string GetFileCrc32(string filePath)
    {
        using (FileStream fs = File.OpenRead(filePath))
        {
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            uint crc = CalculateCrc32(buffer);
            return crc.ToString("X8");  // 格式化为8位大写十六进制字符串
        }
    }
}

