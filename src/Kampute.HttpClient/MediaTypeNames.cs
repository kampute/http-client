// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.HttpClient package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.HttpClient
{
    /// <summary>
    /// Provides constants for common media type names used in MIME content types.
    /// </summary>
    /// <remarks>
    /// This class supplements the standard <see cref="System.Net.Mime.MediaTypeNames"/> with additional, commonly 
    /// used media types that are not covered by the .NET Standard 2.0 specification.
    /// </remarks>
    public static class MediaTypeNames
    {
        /// <summary>
        /// Represents media type names for application content types.
        /// </summary>
        public static class Application
        {
            /// <summary>
            /// Media type name for octet-stream.
            /// </summary>
            public const string Octet = System.Net.Mime.MediaTypeNames.Application.Octet;

            /// <summary>
            /// Media type name for PDF documents.
            /// </summary>
            public const string Pdf = System.Net.Mime.MediaTypeNames.Application.Pdf;

            /// <summary>
            /// Media type name for RTF documents.
            /// </summary>
            public const string Rtf = System.Net.Mime.MediaTypeNames.Application.Rtf;

            /// <summary>
            /// Media type name for SOAP payloads.
            /// </summary>
            public const string Soap = System.Net.Mime.MediaTypeNames.Application.Soap;

            /// <summary>
            /// Media type name for ZIP archives.
            /// </summary>
            public const string Zip = System.Net.Mime.MediaTypeNames.Application.Zip;

            /// <summary>
            /// Media type name for XML data.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER
            public const string Xml = System.Net.Mime.MediaTypeNames.Application.Xml;
#else
            public const string Xml = "application/xml";
#endif

            /// <summary>
            /// Media type name for JSON data.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER
            public const string Json = System.Net.Mime.MediaTypeNames.Application.Json;
#else
            public const string Json = "application/json";
#endif

            /// <summary>
            /// Media type name for BSON data.
            /// </summary>
            public const string Bson = "application/bson";

            /// <summary>
            /// Media type name for YAML data.
            /// </summary>
            public const string Yaml = "application/yaml";

            /// <summary>
            /// Media type name for form URL encoded data.
            /// </summary>
            public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        }

        /// <summary>
        /// Represents media type names for image content types.
        /// </summary>
        public static class Image
        {
            /// <summary>
            /// Media type name for GIF images.
            /// </summary>
            public const string Gif = System.Net.Mime.MediaTypeNames.Image.Gif;

            /// <summary>
            /// Media type name for JPEG images.
            /// </summary>
            public const string Jpeg = System.Net.Mime.MediaTypeNames.Image.Jpeg;

            /// <summary>
            /// Media type name for TIFF images.
            /// </summary>
            public const string Tiff = System.Net.Mime.MediaTypeNames.Image.Tiff;

            /// <summary>
            /// Media type name for PNG images.
            /// </summary>
            public const string Png = "image/png";

            /// <summary>
            /// Media type name for WebP images.
            /// </summary>
            public const string Webp = "image/webp";

            /// <summary>
            /// Media type name for BMP images.
            /// </summary>
            public const string Bmp = "image/bmp";

            /// <summary>
            /// Media type name for HEIC images.
            /// </summary>
            public const string Heic = "image/heic";

            /// <summary>
            /// Media type name for SVG images.
            /// </summary>
            public const string Svg = "image/svg+xml";
        }

        /// <summary>
        /// Represents media type names for text content types.
        /// </summary>
        public static class Text
        {
            /// <summary>
            /// Media type name for HTML documents.
            /// </summary>
            public const string Html = System.Net.Mime.MediaTypeNames.Text.Html;

            /// <summary>
            /// Media type name for plain text documents.
            /// </summary>
            public const string Plain = System.Net.Mime.MediaTypeNames.Text.Plain;

            /// <summary>
            /// Media type name for Rich Text Format (RTF) documents.
            /// </summary>
            public const string RichText = System.Net.Mime.MediaTypeNames.Text.RichText;

            /// <summary>
            /// Media type name for XML documents.
            /// </summary>
            public const string Xml = System.Net.Mime.MediaTypeNames.Text.Xml;

            /// <summary>
            /// Media type name for CSV (Comma-Separated Values) files.
            /// </summary>
            public const string Csv = "text/csv";
        }

        /// <summary>
        /// Represents media type names for audio content types.
        /// </summary>
        public static class Audio
        {
            /// <summary>
            /// Media type name for MPEG Audio Layer 3 (MP3) files.
            /// </summary>
            public const string Mp3 = "audio/mpeg";

            /// <summary>
            /// Media type name for Waveform Audio Format (WAV) files.
            /// </summary>
            public const string Wav = "audio/wav";

            /// <summary>
            /// Media type name for Advanced Audio Coding (AAC) files.
            /// </summary>
            public const string Aac = "audio/aac";

            /// <summary>
            /// Media type name for OGG audio files.
            /// </summary>
            public const string Ogg = "audio/ogg";

            /// <summary>
            /// Media type name for FLAC audio files.
            /// </summary>
            public const string Flac = "audio/flac";

            /// <summary>
            /// Media type name for MIDI audio files.
            /// </summary>
            public const string Midi = "audio/midi";

            /// <summary>
            /// Media type name for Web Audio files.
            /// </summary>
            public const string Webm = "audio/webm";
        }

        /// <summary>
        /// Represents media type names for video content types.
        /// </summary>
        public static class Video
        {
            /// <summary>
            /// Media type name for MPEG Video files.
            /// </summary>
            public const string Mpeg = "video/mpeg";

            /// <summary>
            /// Media type name for MP4 video files.
            /// </summary>
            public const string Mp4 = "video/mp4";

            /// <summary>
            /// Media type name for OGG video files.
            /// </summary>
            public const string Ogg = "video/ogg";

            /// <summary>
            /// Media type name for WebM video files.
            /// </summary>
            public const string Webm = "video/webm";

            /// <summary>
            /// Media type name for QuickTime video files.
            /// </summary>
            public const string QuickTime = "video/quicktime";

            /// <summary>
            /// Media type name for AVI (Audio Video Interleave) files.
            /// </summary>
            public const string Avi = "video/x-msvideo";

            /// <summary>
            /// Media type name for FLV video files.
            /// </summary>
            public const string Flv = "video/x-flv";

            /// <summary>
            /// Media type name for MKV video files.
            /// </summary>
            public const string Mkv = "video/x-matroska";
        }
    }
}
