require "vstudio"
function platformsElement(cfg)
   _p(2,'<Platforms>x64</Platforms>')
end

premake.override(premake.vstudio.cs2005.elements, "projectProperties", function (oldfn, cfg)
   return table.join(oldfn(cfg), {
   platformsElement,
   })
end)


workspace "premake-manager-cli"
architecture "x64"
   configurations { "Debug", "Release" }
   startproject "premake-manager-cli"

   project "premake-manager-cli"
      kind "ConsoleApp" -- CLI application
      dotnetframework "net9.0" -- Targeting .NET 9.0
      location "premake-manager-cli"
      language "C#"
      targetdir "bin/%{cfg.buildcfg}"
      files { "%{prj.name}/src/**.cs" } -- Include all C# source files
      nuget { "Spectre.Console:0.50.0", "Spectre.Console.Cli:0.50.0", "Octokit:14.0.0", "YamlDotNet:16.3.0", "StreamJsonRpc:2.22.11" }
      vsprops {
         PublishSingleFile = "true",
         SelfContained = "true",
         IncludeNativeLibrariesForSelfExtract = "true"
      }
      filter "configurations:Debug"
         defines { "DEBUG" }
         optimize "Off"
      
      filter "configurations:Release"
         symbols "Off"
         defines { "NDEBUG" }
         optimize "On"
