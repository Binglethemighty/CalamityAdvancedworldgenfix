﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;

namespace CalamityMod.Schematics
{
    #region Structs
    // A struct parallel to Tile which, for modded tiles, stores offset tile and wall type IDs based on the schematic's mod name arrays.
    public struct SchematicMetaTile
    {
        // If InputTile.TileType >= TileID.Count, is a modded tile type; consult mod tile name array
        // If InputTile.WallType >= WallID.Count, is a modded wall type; consule mod wall name array
        public Tile storedTile;

        internal readonly ushort originalType;
        internal readonly ushort originalWall;
        public bool keepTile;
        public bool keepWall;

        public SchematicMetaTile(Tile t)
        {
            storedTile = default;
            CalamitySchematicIO.CopyTile(ref storedTile, t);
            originalType = storedTile.TileType; // This is never changed
            originalWall = storedTile.WallType; // This is never changed
            keepTile = false;
            keepWall = false;
        }

        // This function is used by the schematic placer to respect the keepTile and keepWall booleans.
        public void ApplyTo(int x, int y, Tile original)
        {
            Tile target = Main.tile[x, y];
            if (!keepTile && !keepWall) // full overwrite
                CalamitySchematicIO.CopyTile(ref target, storedTile);
            else if (keepTile && keepWall) // full preservation
                CalamitySchematicIO.CopyTile(ref target, original);
            else if (keepWall) // overwrite from replacement/storage, then bring in the wall from the original
            {
                CalamitySchematicIO.CopyTile(ref target, storedTile);
                target.WallType = original.WallType;
                target.WallFrameX = original.WallFrameX;
                target.WallFrameY = original.WallFrameY;
                target.WallColor = original.WallColor;
            }
            else if (keepTile) // preserve the original, but bring in the wall from replacement/storage
            {
                CalamitySchematicIO.CopyTile(ref target, original);
                target.WallType = storedTile.WallType;
                target.WallFrameX = storedTile.WallFrameX;
                target.WallFrameY = storedTile.WallFrameY;
                target.WallColor = storedTile.WallColor;
            }
        }
    }

    // A container struct for a List of unique tile definitions and an array of indices to those tile definitions.
    public struct SchematicData
    {
        // Schematics assume 1024 or less unique tile definitions for their internal buffer.
        // In real schematics, this number usually doesn't go above about 800.
        // The buffer can be expanded up to 65536, as the indices used for it are 2 bytes.
        // Note this would by definition require a schematic of size at least 256x256, so you should never really get to this point.
        private const int DefaultUniqueTileCount = 1024;
        public const int MaxUniqueTileCount = ushort.MaxValue + 1;
        private const int DefaultModTileCount = 256;
        private const int DefaultModWallCount = 32;

        public readonly IList<SchematicMetaTile> uniqueTiles;
        public readonly IList<string> modTileNames;
        public readonly IList<string> modWallNames;
        public readonly ushort[,] areaIndices;

        public SchematicData(int width, int height)
        {
            // Schematics by default have one "unique tile", which is air -- equivalent to new Tile().
            // This tile is always provided in schematics, even if it isn't used (e.g. your schematic is a solid block of stone).
            // This allows for index zero to always refer to a fully blank tile.
            uniqueTiles = new List<SchematicMetaTile>(DefaultUniqueTileCount)
                {
                    new SchematicMetaTile()
                };
            modTileNames = new List<string>(DefaultModTileCount);
            modWallNames = new List<string>(DefaultModWallCount);
            areaIndices = new ushort[width, height];
        }
    }

    public enum ExportResult
    {
        Success,
        CornerOutOfWorld,
        ZeroArea,
        TooManyUniqueTiles,
    };
    #endregion

    public static class CalamitySchematicIO
    {
        // A generous buffer of 1 megabyte is the default for schematics. If this somehow isn't big enough, they can get bigger.
        private const int SchematicBufferStartingSize = 1048576;

        // If true, written schematics will have all data GZip compressed except for the magic number header.
        public static bool UseCompression = true;

        // This is a 3-byte magic number header for Calamity Schematic Files created with TML 1.3. CA1A5C = "CalaSC"
        // These schematics cannot be read anymore. Attempting to do so produces a harmless schematic with no data.
        private static readonly byte[] SchematicMagicNumberHeader_TML13 = new byte[]
        {
            0xCA,
            0x1A,
            0x5C
        };

        // This is a 3-byte magic number header for Calamity Schematic Files created with TML 1.4. CA145C = "CalaSC" but also "Ca14SC"
        private static readonly byte[] SchematicMagicNumberHeader_TML14 = new byte[]
        {
            0xCA,
            0x14,
            0x5C
        };

        private const byte UncompressedMagicNumber = 0x00;
        private const byte CompressedMagicNumber = 0xC0;
        private const string PreserveTileName = "_";

        // These two fields are set to a non-zero value when the CalamitySchematicExporter mod loads.
        public static ushort PreserveTileID = 0;
        public static ushort PreserveWallID = 0;

        #region Copy Tile
        private static readonly FieldInfo TilePointerID = typeof(Tile).GetField("TileId", BindingFlags.Instance | BindingFlags.NonPublic);

        // Tile.CopyFrom is a pointer reassignment, so manually copying every field is necessary here.
        internal static void CopyTile(ref Tile dest, Tile source, bool copyID = false)
        {
            dest.TileType = source.TileType;
            dest.WallType = source.WallType;
            dest.LiquidAmount = source.LiquidAmount;
            dest.LiquidType = source.LiquidType;
            dest.TileFrameX = source.TileFrameX;
            dest.TileFrameY = source.TileFrameY;
            var sourceMiscState = source.Get<TileWallWireStateData>();
            var destMiscState = dest.Get<TileWallWireStateData>();
            TileWallWireStateBitpack.SetValue(destMiscState, sourceMiscState.NonFrameBits);

            if (copyID)
                TilePointerID.SetValue(dest, TilePointerID.GetValue(source));
        }
        #endregion

        #region Direct Serialization Read/Write

        internal static readonly FieldInfo TileWallWireStateBitpack = typeof(TileWallWireStateData).GetField("bitpack", BindingFlags.Instance | BindingFlags.NonPublic);

        private static SchematicMetaTile ReadSchematicMetaTile(this BinaryReader reader)
        {
            SchematicMetaTile smt = new SchematicMetaTile();
            ref Tile t = ref smt.storedTile;

            t.TileType = reader.ReadUInt16();
            t.WallType = reader.ReadUInt16();

            ref var liquidStruct = ref t.Get<LiquidData>();
            liquidStruct.Amount = reader.ReadByte();
            liquidStruct.LiquidType = reader.ReadByte();

            ref var miscStateStruct = ref t.Get<TileWallWireStateData>();
            miscStateStruct.TileFrameX = reader.ReadInt16();
            miscStateStruct.TileFrameY = reader.ReadInt16();

            // Advised by Chicken Bones to clear runtime bits, not preserve them from existing tile
            TileWallWireStateBitpack.SetValue(miscStateStruct, reader.ReadInt32());

            return smt;
        }

        private static void WriteSchematicMetaTile(this BinaryWriter writer, SchematicMetaTile smt)
        {
            ref Tile t = ref smt.storedTile;

            writer.Write(t.TileType); // perfectly efficient shorthand for t.Get<TileTypeData>();
            writer.Write(t.WallType); // perfectly efficient shorthand for t.Get<WallTypeData>();

            ref var liquidStruct = ref t.Get<LiquidData>();
            writer.Write(liquidStruct.Amount);
            byte liquidID = (byte)liquidStruct.LiquidType;
            writer.Write(liquidID);

            ref var miscStateStruct = ref t.Get<TileWallWireStateData>();
            writer.Write(miscStateStruct.TileFrameX);
            writer.Write(miscStateStruct.TileFrameY);
            // Save only the NonFrameBits. The remainder of the bits are runtime only data that should not be serialized.
            writer.Write(miscStateStruct.NonFrameBits);
        }
        #endregion

        #region Export Helper Methods
        // TODO -- technically this could use vanilla Tilemap
        private static Tile[,] GetTilesInRectangle(Rectangle area)
        {
            Tile[,] tiles = new Tile[area.Width, area.Height];
            for (int i = area.Left; i < area.Right; i++)
                for (int j = area.Top; j < area.Bottom; j++)
                {
                    Tile t = Main.tile[i, j];
                    tiles[i - area.Left, j - area.Top] = t;
                }
            return tiles;
        }

        // This equality is slightly more strict than Tile.isTheSameAs because it checks type, wall and frame on non-active tiles.
        public static bool EqualToMetaTile(this Tile t, SchematicMetaTile smt)
        {
            Tile compTile = smt.storedTile;
            if (t.Get<TileWallWireStateData>().NonFrameBits != compTile.Get<TileWallWireStateData>().NonFrameBits)
                return false;

            if (t.WallType != compTile.WallType || t.LiquidAmount != compTile.LiquidAmount)
                return false;

            if (t.LiquidAmount > 0 && t.LiquidType != compTile.LiquidType)
                return false;

            if (t.TileType != compTile.TileType)
                return false;

            if (Main.tileFrameImportant[t.TileType] && (t.TileFrameX != compTile.TileFrameX || t.TileFrameY != compTile.TileFrameY))
                return false;

            return true;
        }

        private static int GetMetaTileIndex(IList<SchematicMetaTile> metaTiles, ref Tile toSearch)
        {
            int numTiles = metaTiles.Count;
            for (int i = 0; i < numTiles; ++i)
                if (toSearch.EqualToMetaTile(metaTiles[i]))
                    return i;
            return -1;
        }

        private static string GetFullName(this ModTile mt) => $"{mt.Mod.Name}/{mt.Name}";
        private static string GetFullName(this ModWall mw) => $"{mw.Mod.Name}/{mw.Name}";

        private static void ComputeMetaIndices(ref SchematicData schematic, ref SchematicMetaTile smt)
        {
            // Special case: this meta tile demands that the original tile in the destination be preserved.
            // The meta index of this special mod tile name is always zero.
            if (PreserveTileID > 0 && smt.storedTile.TileType == PreserveTileID)
            {
                smt.storedTile.TileType = TileID.Count;
                smt.keepTile = true;
            }
            else if (smt.storedTile.TileType >= TileID.Count)
            {
                ModTile mt = ModContent.GetModTile(smt.storedTile.TileType);
                if (mt != null)
                {
                    // This tile has a valid modded tile. Check if that ModTile is already registered to find its index.
                    string tileFullName = mt.GetFullName();
                    int tileNameIndex = schematic.modTileNames.IndexOf(tileFullName);
                    if (tileNameIndex == -1)
                    {
                        // This modded tile is not yet registered. Register it. The list's previous length is its index.
                        tileNameIndex = schematic.modTileNames.Count;
                        schematic.modTileNames.Add(tileFullName);
                    }

                    // Adjust the meta tile's type so that it points to the mod tile name array.
                    // typeOriginal is unaffected so that future searches will still function.
                    smt.storedTile.TileType = (ushort)(TileID.Count + tileNameIndex);
                }
            }

            // Special case: this meta tile demands that the original wall in the destination be preserved.
            // The meta index of this special mod wall name is always zero.
            if (PreserveWallID > 0 && smt.storedTile.WallType == PreserveWallID)
            {
                smt.storedTile.WallType = WallID.Count;
                smt.keepWall = true;
            }
            else if (smt.storedTile.WallType >= WallID.Count)
            {
                ModWall mw = ModContent.GetModWall(smt.storedTile.WallType);
                if (mw != null)
                {
                    // This tile has a valid modded wall. Check if that ModWall is already registered to find its index.
                    string wallFullName = mw.GetFullName();
                    int wallNameIndex = schematic.modWallNames.IndexOf(wallFullName);
                    if (wallNameIndex == -1)
                    {
                        // This modded wall is not yet registered. Register it. The list's previous length is its index.
                        wallNameIndex = schematic.modWallNames.Count;
                        schematic.modWallNames.Add(wallFullName);
                    }

                    // Adjust the meta tile's wall so that it points to the mod wall name array.
                    // wallOriginal is unaffected so that future searches will still function.
                    smt.storedTile.WallType = (ushort)(WallID.Count + wallNameIndex);
                }
            }
        }

        private static SchematicData ConstructSchematicData(Tile[,] tiles)
        {
            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            SchematicData schematic = new SchematicData(tiles.GetLength(0), tiles.GetLength(1));

            // Add the fixed mod tile and mod wall for index zero. These special tiles preserve the existing tile or wall at that location.
            schematic.modTileNames.Add(PreserveTileName);
            schematic.modWallNames.Add(PreserveTileName);

            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    ref Tile t = ref tiles[x, y];
                    int metaTileIndex = GetMetaTileIndex(schematic.uniqueTiles, ref t);

                    // If this is a new tile not already registered, it must be added to uniqueTiles.
                    if (metaTileIndex == -1)
                    {
                        metaTileIndex = schematic.uniqueTiles.Count;
                        SchematicMetaTile smt = new SchematicMetaTile(t);

                        // If either the tile or the wall are modded, we may need to register that data.
                        // We will surely need to modify the meta tile's type and wall fields if it contains modded data.
                        // This function also handles the special case of preservation tiles/walls.
                        ComputeMetaIndices(ref schematic, ref smt);

                        // With potentially modded data processed, add the meta tile to the list of registered meta tiles.
                        schematic.uniqueTiles.Add(smt);
                    }

                    // There is no reason to continue processing a schematic if it is too complicated.
                    if (metaTileIndex >= SchematicData.MaxUniqueTileCount)
                        goto PostAreaIteration;

                    // Update the area indices so that the tile at this position is the correct reference.
                    schematic.areaIndices[x, y] = (ushort)metaTileIndex;
                }

            PostAreaIteration:

            // If there are too many unique tiles, set the size to zero. This will cause the schematic to fail to export.
            // Even in a schematic of nothing but air, there is one unique tile: air.
            if (schematic.uniqueTiles.Count > SchematicData.MaxUniqueTileCount)
                schematic.uniqueTiles.Clear();

            return schematic;
        }
        #endregion

        #region Export
        public static ExportResult ExportSchematic(Rectangle area)
        {
            if (area.Top < 0 || area.Left < 0 || area.Right >= Main.maxTilesX || area.Bottom >= Main.maxTilesY)
                return ExportResult.CornerOutOfWorld;
            // IsEmpty does NOT return false for all rectangles with zero area.
            if (area.Width <= 0 || area.Height <= 0)
                return ExportResult.ZeroArea;

            Tile[,] tiles = GetTilesInRectangle(area);

            byte[] renderedStream;
            using (MemoryStream stream = new MemoryStream(SchematicBufferStartingSize))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                // 1: 4-byte magic number header
                // This is only included here if compression is turned off
                if (!UseCompression)
                {
                    writer.Write(SchematicMagicNumberHeader_TML14);
                    writer.Write(UncompressedMagicNumber);
                }

                // Calculate the schematic's data. Fail immediately if the schematic has too many unique tiles.
                SchematicData schematic = ConstructSchematicData(tiles);
                if (schematic.uniqueTiles.Count <= 0)
                {
                    writer.Close();
                    return ExportResult.TooManyUniqueTiles;
                }

                // 2: Length of list 3.
                ushort numModTileNames = (ushort)schematic.modTileNames.Count;
                writer.Write(numModTileNames);

                // 3: List of fully qualified modded tile names.
                for (ushort i = 0; i < numModTileNames; ++i)
                    writer.Write(schematic.modTileNames[i]);

                // 4: Length of list 5.
                ushort numModWallNames = (ushort)schematic.modWallNames.Count;
                writer.Write(numModWallNames);

                // 5: List of fully qualified modded wall names.
                for (ushort i = 0; i < numModWallNames; ++i)
                    writer.Write(schematic.modWallNames[i]);

                // 6: Length of list 7.
                ushort numUniqueTiles = (ushort)schematic.uniqueTiles.Count;
                writer.Write(numUniqueTiles);

                // 7: List of definitions of unique tiles. For modded tiles, their types are lookup indices to the mod tile and mod wall name arrays.
                // Specific tiles and walls from the CalamitySchematicExporter mod have hardcoded behavior at this step.
                // They will be replaced with preserve flags by overriding the tile and wall meta indices.
                for (ushort i = 0; i < numUniqueTiles; ++i)
                    writer.WriteSchematicMetaTile(schematic.uniqueTiles[i]);

                // 8: Width and height of array 9.
                ushort tileWidth = (ushort)schematic.areaIndices.GetLength(0);
                writer.Write(tileWidth);
                ushort tileHeight = (ushort)schematic.areaIndices.GetLength(1);
                writer.Write(tileHeight);

                // 9: Array of indices to unique tile definitions.
                for (ushort y = 0; y < tileHeight; ++y)
                    for (ushort x = 0; x < tileWidth; ++x)
                        writer.Write(schematic.areaIndices[x, y]);

                // Compile all serialized data into a byte array for writing.
                renderedStream = stream.ToArray();
            }

            // If compression is enabled, replace the current rendered stream with a re-written compressed one.
            if (UseCompression)
            {
                using MemoryStream gzMem = new MemoryStream(renderedStream.Length);

                // Write the magic number outside the compressed region of data if compression is enabled.
                // This is the only way for a reading algorithm to know this is a compressed schematic.
                gzMem.Write(SchematicMagicNumberHeader_TML14, 0, SchematicMagicNumberHeader_TML14.Length);
                gzMem.WriteByte(CompressedMagicNumber);

                using (GZipStream gz = new GZipStream(gzMem, CompressionLevel.Optimal))
                {
                    gz.Write(renderedStream, 0, renderedStream.Length);
                }
                renderedStream = gzMem.ToArray();
            }

            // To prevent overwrites, every schematic has an incredibly precise timestamp in its filename.
            long fileTimestamp = System.DateTime.Now.ToFileTime();
            string filename = $"schematic_{fileTimestamp}.csch";
            string fullPath = Path.Combine(Main.SavePath, filename);
            File.WriteAllBytes(fullPath, renderedStream);
            return ExportResult.Success;
        }
        #endregion

        #region Import Helper Methods
        private static void ReplaceMetaIndicesWithLoadedIDs(ref SchematicMetaTile smt, string[] modTileNames, string[] modWallNames)
        {
            // If this schematic tile has a modded foreground tile, replace the meta index offset with that modded tile's ID.
            if (smt.storedTile.TileType >= TileID.Count)
            {
                // The first entry in modTileNames is always the special preserver name. If you hit this name, just set the keepTile flag.
                string tileFullName = modTileNames[smt.storedTile.TileType - TileID.Count];
                if (tileFullName == PreserveTileName)
                    smt.keepTile = true;
                else
                {
                    ModContent.SplitName(tileFullName, out string mod, out string tileName);
                    Mod theMod = ModLoader.GetMod(mod);
                    // If that mod isn't loaded, spawn in a TML default UnloadedTile instead.
                    smt.storedTile.TileType = (ushort)(theMod is null ? ModContent.TileType<UnloadedTile>() : theMod.Find<ModTile>(tileName).Type);
                }
            }
            // If this schematic tile has a modded wall, replace the meta index offset with that modded wall's ID.
            if (smt.storedTile.WallType >= WallID.Count)
            {
                // The first entry in modWallNames is always the special preserver name. If you hit this name, just set the keepWall flag.
                string wallFullName = modWallNames[smt.storedTile.WallType - WallID.Count];
                if (wallFullName == PreserveTileName)
                    smt.keepWall = true;
                else
                {
                    ModContent.SplitName(wallFullName, out string mod, out string wallName);
                    Mod theMod = ModLoader.GetMod(mod);
                    // If that mod isn't loaded, spawn in a TML default UnloadedWall instead.
                    smt.storedTile.WallType = (ushort)(theMod is null ? ModContent.WallType<UnloadedWall>() : theMod.Find<ModWall>(wallName).Type);
                }
            }
        }
        #endregion

        #region Import
        public static SchematicMetaTile[,] LoadSchematic(string filename)
        {
            SchematicMetaTile[,] ret = null;
            using (Stream st = CalamityMod.Instance.GetFileStream(filename, true))
            {
                ret = ImportSchematic(st);
            }
            return ret;
        }

        private const string InvalidFormatString = "Provided file is not a valid Calamity Schematic.";
        private const string TML13ValidString = "An attempt was made to load a valid Calamity Schematic for TML 1.3. These files cannot be translated into TML 1.4. The schematic will show up empty.";
        private static SchematicMetaTile[,] ImportSchematic(Stream fileInputStream)
        {
            // 1: Header. First three bytes are a magic number. Fourth byte determines compression.
            byte[] header = fileInputStream.ReadBytes(4);

            bool isTML13Schematic = true;
            bool isTML14Schematic = true;
            for (int i = 0; i < SchematicMagicNumberHeader_TML14.Length; ++i)
            {
                if (header[i] != SchematicMagicNumberHeader_TML13[i])
                    isTML13Schematic = false;

                if (header[i] != SchematicMagicNumberHeader_TML14[i])
                    isTML14Schematic = false;
            }

            // If the schematic is neither TML 1.3 or TML 1.4 format, then it's crap.
            if (!isTML13Schematic && !isTML14Schematic)
                throw new InvalidDataException($"{InvalidFormatString} The magic number signature is invalid.");
            else if (isTML13Schematic)
            {
                CalamityMod.Instance.Logger.Error(TML13ValidString);
                SchematicMetaTile[,] empty = new SchematicMetaTile[0, 0];
                return empty;
            }

            bool compression = false;
            if (header[3] == CompressedMagicNumber)
                compression = true;
            else if (header[3] != UncompressedMagicNumber)
                throw new InvalidDataException($"{InvalidFormatString} The file is not properly marked as compressed or uncompressed.");

            SchematicMetaTile[,] ret;
            byte[] buffer;
            using (MemoryStream stream = new MemoryStream(SchematicBufferStartingSize))
            {
                if (compression)
                    using (GZipStream gz = new GZipStream(fileInputStream, CompressionMode.Decompress))
                        gz.CopyTo(stream);
                else
                    fileInputStream.CopyTo(stream);
                buffer = stream.ToArray();
            }

            using (MemoryStream bufferStream = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(bufferStream, Encoding.UTF8))
            {
                // 2: Length of list 3.
                ushort numModTileNames = reader.ReadUInt16();

                // 3: List of fully qualified modded tile names.
                string[] modTileNames = new string[numModTileNames];
                for (int i = 0; i < numModTileNames; ++i)
                    modTileNames[i] = reader.ReadString();

                // 4: Length of list 5.
                ushort numModWallNames = reader.ReadUInt16();
                string[] modWallNames = new string[numModWallNames];

                // 5: List of fully qualified modded wall names.
                for (int i = 0; i < numModWallNames; ++i)
                    modWallNames[i] = reader.ReadString();

                // 6: Length of list 7.
                ushort numUniqueTiles = reader.ReadUInt16();
                SchematicMetaTile[] uniqueTiles = new SchematicMetaTile[numUniqueTiles];

                // 7: List of definitions of unique tiles. For modded tiles, their types are lookup indices to the mod tile and mod wall name arrays.
                for (ushort i = 0; i < numUniqueTiles; ++i)
                {
                    SchematicMetaTile smt = reader.ReadSchematicMetaTile();
                    ReplaceMetaIndicesWithLoadedIDs(ref smt, modTileNames, modWallNames);
                    uniqueTiles[i] = smt;
                }

                // 8: Width and height of array 9.
                ushort tileWidth = reader.ReadUInt16();
                ushort tileHeight = reader.ReadUInt16();
                ret = new SchematicMetaTile[tileWidth, tileHeight];

                // 9: Array of indices to unique tile definitions. We immediately convert these to tiles.
                for (ushort y = 0; y < tileHeight; ++y)
                    for (ushort x = 0; x < tileWidth; ++x)
                    {
                        ushort tileIndex = reader.ReadUInt16();
                        ret[x, y] = uniqueTiles[tileIndex];
                    }
            }
            return ret;
        }
        #endregion
    }
}
