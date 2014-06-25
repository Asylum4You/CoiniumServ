﻿/*
 *   CoiniumServ - crypto currency pool software - https://github.com/CoiniumServ/CoiniumServ
 *   Copyright (C) 2013 - 2014, Coinium Project - http://www.coinium.org
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Numerics;
using Coinium.Coin.Coinbase;
using Coinium.Common.Extensions;
using Coinium.Crypto;
using Coinium.Server.Stratum.Notifications;
using Numerics;
using Serilog;

namespace Coinium.Mining.Shares
{
    public class Share : IShare
    {
        public bool Valid { get; private set; }
        public IJob Job { get; private set; }
        public UInt32 nTime { get; private set; }
        public UInt32 Nonce { get; private set; }
        public UInt32 ExtraNonce1 { get; private set; }
        public UInt32 ExtraNonce2 { get; private set; }
        public byte[] CoinbaseBuffer { get; private set; }
        public Hash CoinbaseHash { get; private set; }
        public byte[] MerkleRoot { get; private set; }
        public byte[] HeaderBuffer { get; private set; }
        public byte[] HeaderHash { get; private set; }
        public BigInteger HeaderValue { get; private set; }
        public Double Difficulty { get; private set; }
        public double BlockDiffAdjusted { get; private set; }
        public bool Candicate { get; private set; }
        public byte[] BlockHex { get; private set; }
        public byte[] BlockHash { get; private set; }

        public Share(UInt64 jobId, IJob job, UInt32 extraNonce1, string extraNonce2, string nTimeString, string nonceString)
        {
            Valid = true; // we'll flag the share as invalid if requred later.

            // set associated job

            Job = job;

            if (Job == null)
            {
                Valid = false;
                Log.Warning("Job doesn't exist: {0}", jobId);
                return;
            }

            // miner supplied parameters

            if (nTimeString.Length != 8)
            {
                Valid = false;
                Log.Warning("Incorrect size of nTime");
                return;
            }

            nTime = Convert.ToUInt32(nTimeString, 16); // ntime for the share

            if (nonceString.Length != 8)
            {
                Valid = false;
                Log.Warning("incorrect size of nonce");
                return;
            }

            Nonce = Convert.ToUInt32(nonceString, 16); // nonce supplied by the miner for the share.

            // job supplied parameters.
            ExtraNonce1 = extraNonce1; // extra nonce1 assigned to job.
            ExtraNonce2 = Convert.ToUInt32(extraNonce2, 16); // extra nonce2 assigned to job.

            // construct the coinbase.
            CoinbaseBuffer = Serializers.SerializeCoinbase(Job, ExtraNonce1, ExtraNonce2); 
            CoinbaseHash = Coin.Coinbase.Utils.HashCoinbase(CoinbaseBuffer);

            // create the merkle root.
            MerkleRoot = Job.MerkleTree.WithFirst(CoinbaseHash).ReverseBuffer();

            // create the block headers
            HeaderBuffer = Serializers.SerializeHeader(Job, MerkleRoot, nTime, Nonce);
            HeaderHash = Job.HashAlgorithm.Hash(HeaderBuffer);
            HeaderValue = new BigInteger(HeaderHash);

            // calculate the share difficulty
            Difficulty = ((double)new BigRational(Job.HashAlgorithm.Difficulty, HeaderValue)) * Job.HashAlgorithm.Multiplier;

            // calculate the block difficulty
            BlockDiffAdjusted = Job.Difficulty * Job.HashAlgorithm.Multiplier;

            // check if block candicate
            if (Job.Target > HeaderValue)
            {
                Candicate = true;
                BlockHex = Serializers.SerializeBlock(Job, HeaderBuffer, CoinbaseBuffer);
                // BlockHash = 
            }
            else
            {
                Candicate = false;
                BlockHash = HeaderBuffer.DoubleDigest().ReverseBuffer();

                // Check if share difficulty reaches miner difficulty.
                if (Difficulty / 16 < 0.99)
                {
                    
                }
            }
        }
    }
}