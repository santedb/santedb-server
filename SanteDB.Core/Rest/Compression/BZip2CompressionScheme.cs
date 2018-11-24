﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: justin
 * Date: 2018-11-23
 */
using SharpCompress.Compressors.BZip2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Compression
{
    /// <summary>
    /// BZip2 Compression stream
    /// </summary>
    public class BZip2CompressionScheme : ICompressionScheme
    {
        /// <summary>
        /// Get the encoding
        /// </summary>
        public string Encoding
        {
            get
            {
                return "bzip2";
            }
        }

        /// <summary>
        /// Create compression stream
        /// </summary>
        public Stream CreateCompressionStream(Stream underlyingStream)
        {
            return new BZip2Stream(underlyingStream, SharpCompress.Compressors.CompressionMode.Compress, true);
        }

        /// <summary>
        /// Create decompression stream
        /// </summary>
        public Stream CreateDecompressionStream(Stream underlyingStream)
        {
            return new BZip2Stream(underlyingStream, SharpCompress.Compressors.CompressionMode.Decompress, true);

        }
    }
}