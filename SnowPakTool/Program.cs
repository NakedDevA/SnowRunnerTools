﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SnowPakTool {

	public static class Program {

		/*
			/packcb "D:\Games\SnowRunner_backs\settings\keys\tmp" "D:\Games\SnowRunner_backs\settings\keys\initial.1.cache_block"
			/unpackcb "D:\Games\SnowRunner_backs\settings\keys\initial.cache_block"
			/zippak "D:\Games\SnowRunner\en_us\preload\paks\client\initial" "D:\Games\SnowRunner_backs\__mod\test.pak"
		*/

		public static int Main ( string[] args ) {
			switch ( args.Length > 0 ? args[0] : null ) {

				case "/license":
					PrintLicense ();
					return 0;

				case "/listcb":
					ListCacheBlock ( args );
					return 0;

				case "/unpackcb":
					UnpackCacheBlock ( args );
					return 0;

				case "/packcb":
					PackCacheBlock ( args );
					return 0;

				case "/zippak":
					ZipPakHelper.CreatePak ( args[1] , args[2] );
					return 0;

				case "/lltest":
					LoadListTest ( args[1] );
					return 0;

				default:
					PrintHelp ();
					return 1;
			}
		}

		[DebuggerDisplay ( "{InternalName} @{Offset,X}" )]
		private sealed class LoadListEntry {
			public long Offset { get; set; }
			public string InternalName { get; set; }
			public string Extension { get; set; }
			public string PakName { get; set; }
			public int MagicA { get; set; }
			public int MagicB { get; set; }
			public byte[] MagicC { get; set; }
		}

		private static void LoadListTest ( string loadListLocation ) {
			using var stream = File.OpenRead ( loadListLocation );
			stream.Position = 0x0000E2A7;
			var entries = new List<LoadListEntry> ();
			while ( stream.Position < stream.Length ) {
				var entry = new LoadListEntry { Offset = stream.Position };
				entry.InternalName = stream.ReadLength32String ();
				entry.Extension = stream.ReadLength32String ();
				entry.PakName = stream.ReadLength32String ();
				entry.MagicA = stream.ReadInt32 ();
				entry.MagicB = stream.ReadInt32 ();
				entry.MagicC = stream.ReadByteArray ( 5 );
				entries.Add ( entry );
			}
		}

		private static void PrintLicense () {
			using var stream = typeof ( Program ).Assembly.GetManifestResourceStream ( $"{nameof ( SnowPakTool )}.LICENSE" );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

		private static void ListCacheBlock ( string[] args ) {
			using ( var stream = File.OpenRead ( args[1] ) ) {
				var reader = new CacheBlockReader ( stream );
				Console.WriteLine ( $"File entries: {reader.FileEntries.Length}" );
				Console.WriteLine ( $"Base offset: {reader.BaseOffset}" );
				var i = 0;
				foreach ( var item in reader.FileEntries ) {
					Console.WriteLine ( $"[{i}] {item.InternalName}: {item.Size} byte(s) at {item.RelativeOffset + reader.BaseOffset}" );
					i++;
				}
			}
		}

		private static void UnpackCacheBlock ( string[] args ) {
			var sourceLocation = Path.GetFullPath ( args[1] );
			var sourceDirectory = Path.GetDirectoryName ( sourceLocation );
			var targetDirectory = args.Length >= 3
						? Path.GetFullPath ( args[2] )
						: Path.Combine ( sourceDirectory , Path.GetFileNameWithoutExtension ( sourceLocation ) );
			if ( Directory.Exists ( targetDirectory ) ) throw new IOException ( $"Target directory '{targetDirectory}' already exists." );
			using ( var stream = File.OpenRead ( sourceLocation ) ) {
				var reader = new CacheBlockReader ( stream );
				reader.UnpackAll ( targetDirectory );
			}
		}

		private static void PackCacheBlock ( string[] args ) {
			var sourceDirectory = Path.GetFullPath ( args[1] );
			var targetLocation = args[2];
			var entries = CacheBlockWriter.GetFileEntries ( sourceDirectory ).OrderBy ( a => a.InternalName ).ToList ();
			using ( var stream = File.Open ( targetLocation , FileMode.CreateNew , FileAccess.Write , FileShare.Read ) ) {
				var writer = new CacheBlockWriter ( stream );
				writer.Pack ( sourceDirectory , entries );
			}
		}

		private static void PrintHelp () {
			Console.WriteLine ( "Usage:" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /license" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listcb file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /unpackcb file.cache_block [directory]" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /packcb directory file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /zippak directory file.pak" );
		}

	}

}
