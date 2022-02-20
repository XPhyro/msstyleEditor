﻿using libmsstyle;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libmsstyleTests
{
    [TestClass]
    public class LoadSaveConsistency
    {
        [DataTestMethod]
        [DataRow(@"..\..\..\styles\w7_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w81_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w10_1709_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w10_1809_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w10_1903_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w10_20h2_aero.msstyles")]
        [DataRow(@"..\..\..\styles\w11_pre_aero.msstyles")]
        public void VerifyLoadSave(string file)
        {
            using(var original = new VisualStyle())
            using(var saved = new VisualStyle())
            {
                original.Load(file);
                original.Save("tmp.msstyles");
                saved.Load("tmp.msstyles");

                bool result = CompareStyles(original, saved);
                Assert.IsTrue(result);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            { 
                File.Delete("tmp.msstyles");
            }
            catch (Exception) { }
        }

        void LogMessage(ConsoleColor c, string message, params object[] args)
        {
            var tmp = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.Write(String.Format(message, args));
            Console.ForegroundColor = tmp;
        }

        bool CompareStyles(VisualStyle s1, VisualStyle s2)
        {
            bool result = true;

            // for all classes in the original style
            foreach (var cls in s1.Classes)
            {
                // see if there is one in the reloaded one
                StyleClass clsOther;
                if(s2.Classes.TryGetValue(cls.Key, out clsOther))
                {
                    // foreach part in the original classes
                    foreach (var part in cls.Value.Parts)
                    {
                        // see if there is an equivalent one in the reloaded classes
                        StylePart partOther;
                        if(clsOther.Parts.TryGetValue(part.Key, out partOther))
                        {
                            // foreach state in all original parts
                            foreach (var state in part.Value.States)
                            {
                                // see if it exists in the reloaded parts as well
                                StyleState stateOther;
                                if(partOther.States.TryGetValue(state.Key, out stateOther))
                                {
                                    // foreach properties in all original states
                                    foreach (var prop in state.Value.Properties)
                                    {
                                        // see if the property exists, by just comparing the header
                                        var propOther = stateOther.Properties.FindAll((p) => p.Header.Equals(prop.Header));
                                        if (propOther.Count == 0)
                                        {
                                            result = false;
                                            LogMessage(ConsoleColor.DarkRed, "Missing prop [N: {0}, T: {1}], in\r\n", prop.Header.nameID, prop.Header.typeID);
                                            LogMessage(ConsoleColor.DarkRed, "State {0}: {1}\r\n", state.Value.StateId, state.Value.StateName);
                                            LogMessage(ConsoleColor.DarkRed, "Part {0}: {1}\r\n", part.Value.PartId, part.Value.PartName);
                                            LogMessage(ConsoleColor.DarkRed, "Class {0}: {1}\r\n", cls.Value.ClassId, cls.Value.ClassName);
                                            continue;
                                        }

                                        var valueEqual = propOther.Any((p) =>
                                        {
                                            bool eq = false;
                                            if (p.GetValue() is List<Int32> li)
                                            {
                                                eq = Enumerable.SequenceEqual(li, (List<Int32>)prop.GetValue());
                                            }
                                            else if (p.GetValue() is List<Color> lc)
                                            {
                                                eq = Enumerable.SequenceEqual(lc, (List<Color>)prop.GetValue());
                                            }
                                            else
                                            {
                                                eq = p.GetValue().Equals(prop.GetValue());
                                            }
                                            return eq;
                                        });

                                        if (!valueEqual)
                                        {
                                            result = false;
                                            LogMessage(ConsoleColor.DarkRed, "Different value for Prop [N: {0}, T: {1}], in\r\n", prop.Header.nameID, prop.Header.typeID);
                                            LogMessage(ConsoleColor.DarkRed, "State {0}: {1}\r\n", state.Value.StateId, state.Value.StateName);
                                            LogMessage(ConsoleColor.DarkRed, "Part {0}: {1}\r\n", part.Value.PartId, part.Value.PartName);
                                            LogMessage(ConsoleColor.DarkRed, "Class {0}: {1}\r\n", cls.Value.ClassId, cls.Value.ClassName);
                                        }
                                    }
                                }
                                else
                                {
                                    result = false;
                                    LogMessage(ConsoleColor.DarkRed, "Missing state {0}: {1}, in\r\n", state.Value.StateId, state.Value.StateName);
                                    LogMessage(ConsoleColor.DarkRed, "Part {0}: {1}\r\n", part.Value.PartId, part.Value.PartName);
                                    LogMessage(ConsoleColor.DarkRed, "Class {0}: {1}\r\n", cls.Value.ClassId, cls.Value.ClassName);
                                }
                            }
                        }
                        else
                        {
                            result = false;
                            LogMessage(ConsoleColor.DarkRed, "Missing part {0}: {1}, in\r\n", part.Value.PartId, part.Value.PartName);
                            LogMessage(ConsoleColor.DarkRed, "Class {0}: {1}\r\n", cls.Value.ClassId, cls.Value.ClassName);
                        }
                    }
                }
                else
                {
                    result = false;
                    LogMessage(ConsoleColor.DarkRed, "Missing class {0}: {1}\r\n", cls.Value.ClassId, cls.Value.ClassName);
                }
            }

            return result;
        }
    }
}
