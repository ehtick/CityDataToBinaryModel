﻿/*
*  Copyright (C) X Gemeente
*              	 X Amsterdam
*				 X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://joinup.ec.europa.eu/software/page/eupl
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using TileBakeLibrary;

namespace TileBakeTool
{
	class Program
	{
		private static ConfigFile configFile;

		private static string sourcePathOverride = "";
		private static string outputPathOverride = "";
		private static float lodOverride = 1;

		static void Main(string[] args)
		{
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //No parameters or an attempt to call for help? Show help in console.
            if (args.Length == 0 || (args.Length == 1 && args[0].ToLower().Contains("help")))
            {
                ShowHelp();
            }
            //One parameter? Assume its a config file path
            else if (args.Length == 1)
            {
                ApplyConfigFileSettings(args[0]);
            }
             //More parameters? Parse them
            else
            {
                ParseArguments(args);
            }

            //If we received the minimal settings to start, start converting!
            if (configFile != null)
                StartConverting();
        }

		private static void ParseArguments(string[] args)
		{
			//Read the arguments and apply corresponding settings
			for (int i = 0; i < args.Length; i++)
			{
				var argument = args[i];
				if (argument.Contains("--")){
					var value = (i + 1 < args.Length) ? args[i + 1] : "";
					ApplySetting(argument,value);
				}
			}
		}

		/// <summary>
		/// Load a .json config file and apply it to our settings object
		/// </summary>
		/// <param name="configFilePath">Path to config file</param>
		private static void ApplyConfigFileSettings(string configFilePath){
			if(File.Exists(configFilePath))
			{
				var configJsonText = File.ReadAllText(configFilePath);
				configFile = JsonSerializer.Deserialize<ConfigFile>(configJsonText
				, new JsonSerializerOptions()
				{ 
					AllowTrailingCommas = true }
				);
				Console.WriteLine($"Loaded config file: {Path.GetFileName(configFilePath)}");
			}
		}


		/// <summary>
		/// Apply commandline parameters as settings
		/// </summary>
		/// <param name="argument">Commandline parameter argument</param>
		/// <param name="value">Commandline parameter value</param>
		private static void ApplySetting(string argument, string value)
		{
			switch (argument)
			{
				case "--config":
					ApplyConfigFileSettings(value);
					break;
				case "--source":
					sourcePathOverride = value;
					Console.WriteLine($"Source: {value}");
					break;
				case "--output":
					outputPathOverride = value;
					Console.WriteLine($"Output directory: {value}");
					break;
				case "--lod":
					lodOverride = float.Parse(value,System.Globalization.CultureInfo.InvariantCulture);
					Console.WriteLine($"LOD filter: {lodOverride}");
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Start the converting process using the current configuration
		/// </summary>
		private static void StartConverting()
		{
			Console.WriteLine("Starting...");

			//Here we use the .dll. This way we may use this library in Unity, or an Azure C# Function
			var tileBaker = new CityJSONToTileConverter();
			tileBaker.SetSourcePath((sourcePathOverride != "") ? sourcePathOverride : configFile.sourceFolder);
			tileBaker.SetTargetPath((outputPathOverride != "") ? outputPathOverride : configFile.outputFolder);
			tileBaker.SetLOD(configFile.lod);
			tileBaker.SetVertexMergeAngleThreshold(configFile.mergeVerticesBelowAngle);
			tileBaker.SetID(configFile.identifier, configFile.removePartOfIdentifier);
			tileBaker.SetReplace(configFile.replaceExistingObjects);
			tileBaker.SetExportUV(configFile.exportUVCoordinates);
			tileBaker.AddBrotliCompressedFile(configFile.brotliCompression);
			tileBaker.SetClipSpikes(configFile.removeSpikes, configFile.removeSpikesAbove, configFile.removeSpikesBelow);
			tileBaker.SetObjectFilters(configFile.cityObjectFilters);
			tileBaker.SetTileSize(configFile.tileSize);
			tileBaker.TilingMethod = configFile.tilingMethod;

			tileBaker.Convert();
		}


		/// <summary>
		/// Draw the help text in the commandline
		/// </summary>
		private static void ShowHelp()
		{
			Console.Write(Constants.helpText);
		}
	}
}
