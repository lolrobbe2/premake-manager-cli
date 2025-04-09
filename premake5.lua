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
   configurations { "Debug", "Release" }
   language "C#"
   dotnetframework "net9.0" -- Targeting .NET 9.0
   startproject "premake-manager-cli"

   project "premake-manager-cli"
      kind "ConsoleApp" -- CLI application
      location "premake-manager-cli"
      language "C#"
      dotnetsdk "Default"
      targetdir "bin/%{cfg.buildcfg}"
      nuget { "Cocona:2.2.0" }
      files { "src/**.cs" } -- Include all C# source files

      vsprops {
         PublishSingleFile = "true",
         SelfContained = "true"
      }
      filter "configurations:Debug"
         defines { "DEBUG" }
         optimize "Off"
      
      filter "configurations:Release"
         defines { "NDEBUG" }
         optimize "On"
