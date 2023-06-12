/*
* MIT License
* 
* Copyright (c) 2022 FZFalzar
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NikkeModManagerCore {
    class NikkeTools {
        internal class Header {
            public byte[] Magic { get; set; }
            public uint Version { get; set; }
            public short HeaderSize { get; set; }
            public short EncryptionMode { get; set; }
            public short KeyLength { get; set; }
            public short EncryptedLength { get; set; }

            public byte[] Key { get; set; }
            public byte[] Iv { get; set; }

            public override string ToString() {
                return $"Version: {Version:X2}\nHeaderSize: {HeaderSize:X2}\nEncryptionMode: {EncryptionMode:X2}\nKeyLength: {KeyLength:X2}\nEncryptedLength: {EncryptedLength:X2}";
            }
        }

        public static Stream DecryptBundle(string path) {
            return DecryptBundle(File.Open(path, FileMode.Open));
        }
        public static Stream DecryptBundle(Stream stream) {
            byte[] headermagic = new byte[] {
                0x4e, 0x4b, 0x41, 0x42
            };
            try {
                MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                byte[] input = memoryStream.ToArray();
                memoryStream.Seek(0, SeekOrigin.Begin);

                using BinaryReader reader = new BinaryReader(memoryStream);
                Header header = new Header();
                header.Magic = reader.ReadBytes(4);
                if (!header.Magic.SequenceEqual(headermagic))
                    throw new FileLoadException("Not NKAB!");

                header.Version = reader.ReadUInt32();
                switch (header.Version) {
                    case 1:
                        return DecryptV1(input, header, reader);
                    case 2:
                        return DecryptV2(input, header, reader);
                    default:
                        Console.WriteLine($"Error processing, unknown asset version encountered: ");
                        break;
                }
            } catch (FileLoadException) {
                Console.WriteLine($"stream does not seem to be NKAB!");
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            return new MemoryStream();
        }

        private static Stream DecryptV1(byte[] input, Header header, BinaryReader reader) {
            header.HeaderSize = (short)(reader.ReadInt16() + 100);
            header.EncryptionMode = (short)(reader.ReadInt16() + 100);
            header.KeyLength = (short)(reader.ReadInt16() + 100);
            header.EncryptedLength = (short)(reader.ReadInt16() + 100);

            header.Key = reader.ReadBytes(header.KeyLength);
            header.Iv = reader.ReadBytes(header.KeyLength);

            var sha = SHA256.Create();
            var hashed = sha.ComputeHash(header.Key);

            var encrypted = reader.ReadBytes(header.EncryptedLength);
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            var decryptor = aes.CreateDecryptor(hashed, header.Iv);
            byte[] outBuf = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            byte[] remainderBuf = input.AsSpan((int)reader.BaseStream.Position).ToArray();
            Stream fs = new MemoryStream();
            fs.Write(outBuf);
            fs.Write(remainderBuf);
            fs.Seek(0, SeekOrigin.Begin);
            return fs;
        }

        private static Stream DecryptV2(byte[] input, Header header, BinaryReader reader) {
            // read last 32 bytes of file
            var currentPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(-32, SeekOrigin.End);
            header.Key = reader.ReadBytes(32);

            // the first 2 bytes of x is the number required to decode the header
            var obfNumber = BitConverter.ToInt16(header.Key);

            // apply the rest of the operation
            reader.BaseStream.Position = currentPos;
            header.HeaderSize = (short)(reader.ReadInt16() + obfNumber);
            header.EncryptionMode = (short)(reader.ReadInt16() + obfNumber);
            header.KeyLength = (short)(reader.ReadInt16() + obfNumber);
            header.EncryptedLength = (short)(reader.ReadInt16() + obfNumber);
            header.Iv = reader.ReadBytes(16);

            var encrypted = reader.ReadBytes(header.EncryptedLength);
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            var decryptor = aes.CreateDecryptor(header.Key, header.Iv);
            byte[] outBuf = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            byte[] remainderBuf = input.AsSpan((int)reader.BaseStream.Position).ToArray();
            Stream fs = new MemoryStream();
            fs.Write(outBuf);
            fs.Write(remainderBuf, 0, remainderBuf.Length - 32);
            fs.Seek(0, SeekOrigin.Begin);
            return fs;
        }

        static Stream EncryptBundleV1(string path) {
            byte[] input = null;
            try {
                input = File.ReadAllBytes(path);
            } catch (Exception e) {
                Console.WriteLine($"Error reading from {path}");
                Console.WriteLine(e.ToString());
            }

            try {

                using MemoryStream ms = new MemoryStream();
                using BinaryWriter writer = new BinaryWriter(ms);
                var sha = SHA256.Create();
                var inputHash = sha.ComputeHash(input);

                Header header = new Header();
                header.Key = System.Text.Encoding.UTF8.GetBytes("ModdedNIKKEAsset");
                header.Iv = inputHash.AsSpan(0, 16).ToArray();

                header.Magic = new byte[] { 0x4e, 0x4b, 0x41, 0x42 };
                header.Version = 1;

                // magic(0x4) + ver(0x4) + fields(0x2 * 4) + key(0x10) + IV(0x10)
                header.HeaderSize = 48;                         // field 1
                header.EncryptionMode = 0;                      // field 2
                header.KeyLength = (short)header.Key.Length;    // field 3
                header.EncryptedLength = 128;                   // field 4

                // write header size, encryption mode, key length, encrypted length
                var keyHash = sha.ComputeHash(header.Key);
                var headerSlice = input.AsSpan(0, header.EncryptedLength).ToArray();

                // encrypt
                Aes aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                var encryptor = aes.CreateEncryptor(keyHash, header.Iv);
                var encrypted = encryptor.TransformFinalBlock(headerSlice, 0, headerSlice.Length);

                // write header
                writer.Write(header.Magic);
                writer.Write(header.Version);
                writer.Write((short)(header.HeaderSize - 100));
                writer.Write((short)(header.EncryptionMode - 100));
                writer.Write((short)(header.KeyLength - 100));
                writer.Write((short)(header.EncryptedLength - 100));
                writer.Write(header.Key);
                writer.Write(header.Iv);

                // write encrypted contents
                writer.Write(encrypted);

                // write remainder
                writer.Write(input.AsSpan(header.EncryptedLength));

                // end
                Stream fs = new MemoryStream();
                fs.Write(ms.GetBuffer());
                return fs;
            } catch (Exception e) {
                Console.WriteLine("Error processing file: ");
                Console.WriteLine(e.ToString());
                return new MemoryStream();
            }
        }
    }
}
