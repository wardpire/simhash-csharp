using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SimhashLib
{
    public class SimhashIndex
    {
        public int kDistance { get; private set; }
        public int SizeInBits { get; private set; }
        
        private Dictionary<string, HashSet<KeyValuePair<long, byte[]>>> _bucket;

        //whitepaper says 64 and 3 are optimal. the ash tray says you've been up all night...
        public SimhashIndex(Dictionary<long, Simhash> objs, int sizeInBits = 64, int k = 3)
        {
            this.kDistance = k;
            SetFpSize(sizeInBits);
            var bucketHashSet = new HashSet<string>();
            _bucket = new();

            foreach (KeyValuePair<long, Simhash> q in objs)
            {
                Add(q.Key, q.Value);
            }
        }

        private void SetFpSize(int fpSize)
        {
            if (fpSize % 8 != 0)
                throw new ArgumentException("Argument 'sizeInBits' has to be a multiple of 8");
            SizeInBits = fpSize;
        }

        public HashSet<long> GetNearDups(Simhash simhash)
        {
            /*
            "simhash" is an instance of Simhash
            return a list of obj_id, which is in type of long (for now)
            */
            if (simhash.SizeInBits != this.SizeInBits) throw new Exception($"Simhash must have same size as index {simhash.SizeInBits}x{SizeInBits}");

            var ans = new HashSet<long>();

            foreach (var key in GetKeysInternal(simhash))
            {
                if (_bucket.ContainsKey(key))
                {
                    var dups = _bucket[key];
                    foreach (var dup in dups)
                    {
                        var sim2 = new Simhash(dup.Value);
                        int d = simhash.Distance(sim2);
                        if (d <= kDistance)
                        {
                            ans.Add(dup.Key);
                        }
                    }
                }
            }
            return ans;
        }
        public void Add(long obj_id, Simhash simhash)
        {
            foreach (string key in GetKeysInternal(simhash))
            {
                var v = new KeyValuePair<long, byte[]>(obj_id, simhash.Value);
                if (!_bucket.ContainsKey(key))
                {
                    var bucketHashSet = new HashSet<KeyValuePair<long, byte[]>>() { v };
                    _bucket.Add(key, bucketHashSet);
                }
                else
                {
                    var values = _bucket[key];
                    values.Add(v);
                }
            }
        }

        public void Delete(long obj_id, Simhash simhash)
        {
            if (simhash.SizeInBits != SizeInBits)
                return;

            foreach (string key in GetKeysInternal(simhash))
            {
                var v = new KeyValuePair<long, byte[]>(obj_id, simhash.Value);
                if (_bucket.ContainsKey(key))
                {
                    _bucket[key].RemoveWhere(x =>
                    {
                        if (x.Key != obj_id)
                            return false;
                        var allBytesAreSame = true;
                        var bytesCount = simhash.SizeInBits / 8;
                        for (int i = 0; i < bytesCount; i++)
                        {
                            if (simhash.Value[i] != x.Value[i])
                            {
                                allBytesAreSame = false;
                                break;
                            }
                        }
                        return allBytesAreSame;
                    });
                }
            }
        }

        public List<string> GetKeys(Simhash simhash)
        {
            return GetKeysInternal(simhash).ToList();
        }
        private IEnumerable<string> GetKeysInternal(Simhash simhash)
        {
            var bitStep = SizeInBits / kDistance;
            var oneMoreBitStepFromIndex = (kDistance - (SizeInBits % kDistance)) * bitStep;

            //fpSize=6 bitStep=2
            //    i2=0  i1=2  i0=4
            //bits   11    11    10

            //test_get_near_dup_hash
            //  0          9            20           31           42           53           64
            //  1-10010111 110-01101101 111-01010110 011-11000000 100-00011100 101-11100000

            byte mask = 0;
            var lastStoredIndex = simhash.SizeInBits;
            byte currentByte = 0;
            List<byte> buffer = new();
            var positionInOutputByte = 0;
            var keyCounter = 0;
            for (int i = simhash.SizeInBits - 1; i >= 0; i--)
            {
                var localBitSTep = bitStep;
                if (i >= oneMoreBitStepFromIndex)
                    localBitSTep++;

                var positionInSourceByte = (simhash.SizeInBits - 1 - i) % 8;

                mask |= (byte)(1 << positionInSourceByte);

                if ((lastStoredIndex - i) == localBitSTep || i == 0)
                {
                    var snapshot = (byte)(simhash.Value[i / 8] & mask);//snapshot current
                    currentByte |= (byte)(snapshot >> (positionInSourceByte - positionInOutputByte));
                    buffer.Add(currentByte);
                    yield return $"{keyCounter}-{Convert.ToBase64String(buffer.ToArray())}";

                    keyCounter++;
                    currentByte = 0;
                    lastStoredIndex = i;
                    buffer.Clear();
                    positionInOutputByte = 0;
                    mask = 0;
                }
                else
                {
                    if (positionInSourceByte == 7)
                    {
                        var snapshot = (byte)(simhash.Value[i / 8] & mask);//snapshot current part - last chance
                        currentByte |= (byte)(snapshot >> (7 - positionInOutputByte));
                        mask = 0;
                    }

                    if (positionInOutputByte == 7)
                    {
                        var snapshot = (byte)(simhash.Value[i / 8] & mask);//snapshot current - we have whole byte
                        currentByte |= (byte)(snapshot << (7 - positionInSourceByte));
                        buffer.Add(currentByte);
                        currentByte = 0;
                        mask = 0;
                        positionInOutputByte = 0;
                    }
                    else
                        positionInOutputByte++;
                }
            }
        }
    }
}
