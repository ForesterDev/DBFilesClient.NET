﻿using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DBFilesClient.NET.WDBC
{
    internal class Reader<T> : NET.Reader<T> where T : class, new()
    {
        protected override bool EnforceStructureMatch { get; } = false;

        internal Reader(Stream fileStream) : base(fileStream)
        {
        }

        protected override void LoadHeader()
        {
            // We get to this through the Factory, meaning we already read the signature...
            FileHeader.RecordCount = ReadInt32();
            if (FileHeader.RecordCount == 0)
                return;

            BaseStream.Position += 4; // Counts arrays
            FileHeader.RecordSize = ReadInt32();
            FileHeader.StringTableSize = ReadInt32();

            FileHeader.HasStringTable = FileHeader.StringTableSize != 0;
            FileHeader.StringTableOffset = BaseStream.Length - FileHeader.StringTableSize;

            foreach (var propertyInfo in typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.PropertyType.IsArray)
                    FileHeader.FieldCount += propertyInfo.GetCustomAttribute<ArraySizeAttribute>()?.SizeConst ?? 0;
                else
                    ++FileHeader.FieldCount;
            }
        }

        protected override void LoadRecords()
        {
            for (var i = 0; i < FileHeader.RecordCount; ++i)
            {
                var key = ReadInt32();
                BaseStream.Position -= 4;

                TriggerRecordLoaded(key, RecordReader(this));

                BaseStream.Position += FileHeader.RecordSize;
            }
        }
    }
}
