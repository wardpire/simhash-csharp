using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Data.HashFunction.Jenkins;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace SimhashLib
{
    public class Simhash
    {
        public enum HashingType
        {
            MD5,
            Jenkins
        }
        public int SizeInBits { get; private set; }
        public byte[] Value { get; private set; }

        public Simhash(int sizeInBits = 64)
        {
            SetSize(sizeInBits);
        }

        public Simhash(HashingType hashingType, int sizeInBits = 64)
        {
            hashAlgorithm = hashingType;
            SetSize(sizeInBits);
        }

        public Simhash(Simhash simHash)
        {
            Value = simHash.Value;
            SizeInBits = simHash.SizeInBits;
        }
        public Simhash(byte[] fingerPrint)
        {
            Value = fingerPrint;
            SizeInBits = fingerPrint.Length * 8;
        }

        private void SetSize(int sizeInBits)
        {
            if (sizeInBits % 8 != 0)
                throw new ArgumentException("Argument 'sizeInBits' has to be a multiple of 8");
            SizeInBits = sizeInBits;
        }

        public void GenerateSimhash(string content)
        {
            var shingling = new Shingling();
            var shingles = shingling.tokenize(content);
            GenerateSimhash(shingles);
        }

        //playing around with hashing algorithms. turns out md5 is a touch slow.
        private HashingType hashAlgorithm = HashingType.Jenkins;

        public void GenerateSimhash(List<string> features)
        {
            switch (hashAlgorithm)
            {
                case HashingType.MD5:
                    BuildByFeaturesMd5(features);
                    break;
                default:
                    BuildByFeaturesJenkins(features);
                    break;
            }
        }

        public int Distance(Simhash another)
        {
            if (SizeInBits != another.SizeInBits) throw new Exception($"Compared Simhashes differs in size {SizeInBits}x{another.SizeInBits}");

            var bytes = SizeInBits / 8;
            var xorBytes = new byte[bytes];
            for (int i = 0; i < bytes; i++)
            {
                xorBytes[i] = (byte)(Value[i] ^ another.Value[i]);
            }

            int ans = 0;
            bool end = false;
            while (!end)
            {
                for (int i = bytes - 1; i >= 0; i--)
                {
                    if (xorBytes[i] > 0)
                    {
                        xorBytes[i] &= (byte)(xorBytes[i] - 1);
                        break;
                    }
                }
                if (xorBytes.All(x => x == 0))
                    end = true;//zero at least significant byte
                else
                    ans++;
            }
            return ans;
        }

        private void BuildByFeaturesJenkins(List<string> features)
        {
            int[] v = SetupFingerprint();

            foreach (string feature in features)
            {
                var h = HashFuncJenkins(feature);
                int w = 1;
                for (int i = 0; i < SizeInBits; i++)
                {
                    byte result = (byte)(h[i / 8] & (1 << (7 - (i % 8))));
                    v[i] += (result > 0) ? w : -w;
                }
            }

            Value = MakeFingerprint(v);
        }

        private void BuildByFeaturesMd5(List<string> features)
        {
            int[] v = SetupFingerprint();

            foreach (string feature in features)
            {
                //this is using MD5 which is REALLY slow
                byte[] h = HashFuncMd5(feature);
                int w = 1;
                for (int i = 0; i < SizeInBits; i++)
                {
                    byte result = (byte)(h[i / 8] & (1 << (7 - (i % 8))));
                    v[i] += (result > 0) ? w : -w;
                }
            }

            Value = MakeFingerprint(v);
        }


        private byte[] MakeFingerprint(int[] v)
        {
            List<byte> ans = new();
            byte current = 0;
            for (int i = 0; i < SizeInBits; i++)
            {
                if (v[i] >= 0)
                {
                    current |= (byte)(1 << (7 - (i % 8)));
                }

                if (i % 8 == 7)
                {
                    ans.Add(current);
                    current = 0;
                }
            }
            return ans.ToArray();
        }

        private int[] SetupFingerprint()
        {
            int[] v = new int[SizeInBits];
            for (int i = 0; i < v.Length; i++) v[i] = 0;
            return v;
        }

        public byte[] HashFuncJenkins(string x)
        {
            var jenkinsLookup3 = JenkinsLookup3Factory.Instance.Create(new JenkinsLookup3Config() { HashSizeInBits = SizeInBits });
            var resultBytes = jenkinsLookup3.ComputeHash(x);
            return resultBytes.Hash;
        }

        private byte[] HashFuncMd5(string x)
        {
            var prolongedHash = new byte[SizeInBits];
            byte[] md5Data;
            using (MD5 md5Hash = MD5.Create())
            {
                md5Data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(x));
            }
            for (int i = 0; i < SizeInBits; i++)
                prolongedHash[i] = md5Data[i % md5Data.Length];
            return prolongedHash;
        }
    }
}
