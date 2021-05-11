﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Office.Interop.PowerPoint;
//Thanks to CSharpFritz and EngstromJimmy for their gists, snippets, and thoughts.

namespace PowerPointToOBSSceneSwitcher
{
    class Program
    {
        private static readonly Application ppt = new Application();
        private static ObsLocal OBS;

        static async Task Main()
        {
            Console.Write("Connecting to PowerPoint...");
            ppt.SlideShowNextSlide += App_SlideShowNextSlide;
            Console.WriteLine("connected");

            Console.Write("Connecting to OBS...");
            OBS = new ObsLocal();
            await OBS.Connect();
            Console.WriteLine("connected");

            OBS.GetScenes();

            Console.ReadLine();
        }


        static void App_SlideShowNextSlide(SlideShowWindow Wn)
        {
            if (Wn != null)
            {
                Console.WriteLine($"Moved to Slide Number {Wn.View.Slide.SlideNumber}");
                //Text starts at Index 2 ¯\_(ツ)_/¯
                var note = string.Empty;
                try { note = Wn.View.Slide.NotesPage.Shapes[2].TextFrame.TextRange.Text; }
                catch { /*no notes*/ }

                bool sceneHandled = false;


                var notereader = new StringReader(note);
                string line;
                while ((line = notereader.ReadLine()) != null)
                {
                    if (line.StartsWith("OBS:"))
                    {
                        line = line[4..].Trim();

                        if (!sceneHandled)
                        {
                            Console.WriteLine($"  Switching to OBS Scene named \"{line}\"");
                            try
                            {
                                sceneHandled = OBS.ChangeScene(line);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  ERROR: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  WARNING: Multiple scene definitions found.  I used the first and have ignored \"{line}\"");
                        }
                    }

                    if (line.StartsWith("OBSDEF:"))
                    {
                        OBS.DefaultScene = line[7..].Trim();
                        Console.WriteLine($"  Setting the default OBS Scene to \"{OBS.DefaultScene}\"");
                    }

                    if (line.StartsWith("**START"))
                    {
                        OBS.StartRecording();
                    }

                    if (line.StartsWith("**STOP"))
                    {
                        OBS.StopRecording();
                    }

                    if (!sceneHandled)
                    {
                        OBS.ChangeScene(OBS.DefaultScene);
                        Console.WriteLine($"  Switching to OBS Default Scene named \"{OBS.DefaultScene}\"");
                    }
                }
            }
        }

    }
}