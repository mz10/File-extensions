using System;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;

namespace Pripony {
    //asociace pripon
    class ASC {
        public static string nazevPr = "EXT_";

        public static SInfo Nacti(string pripona) {
            Microsoft.Win32.RegistryKey klic;

            string nazev, hl_cesta, typ, ikona, program, misto;
            nazev = hl_cesta = typ = ikona = program = misto = "";

            //hledani asociace v HKCU
            klic = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + pripona + "\\UserChoice\\");

            //vlastni nastaveni asociace ve vychozich programech
            if (klic != null) {
                nazev = klic.GetValue("Progid","").ToString();
                misto = "UP";
            }

            if (nazev == "") {
                klic = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + pripona + "\\OpenWithProgids\\");

                //je hodnota v registru REG_NONE? a jeji nazev
                if (klic != null) {
                    foreach (string hodnota in klic.GetValueNames())
                        if (klic.GetValueKind(hodnota).ToString() == "None") {
                            nazev = hodnota;
                            //break ne!, protoze to bere posledni hodnotu!
                        }
                    misto = "UO";
                }
            }

            if (nazev == "") {//pokud neni nazev asociace v HKCU, hledat v HKCR
                klic = Registry.ClassesRoot.OpenSubKey("." + pripona);
                if (klic != null) nazev = klic.GetValue(null,"").ToString();
            }

            if (nazev != "") {//nazev asociace je vyhledany
                hl_cesta = "Software\\Classes\\" + nazev + "\\";

                klic = Registry.CurrentUser.OpenSubKey(hl_cesta);
                if (klic != null) {//je vytvorena asociace v HKCU?
                    typ = klic.GetValue(null,"").ToString();

                    klic = Registry.CurrentUser.OpenSubKey(hl_cesta + "DefaultIcon\\");
                    if (klic != null) ikona = klic.GetValue(null,"").ToString();

                    klic = Registry.CurrentUser.OpenSubKey(hl_cesta + "shell\\open\\command\\");
                    if (klic != null) program = klic.GetValue(null,"").ToString();

                    //pokud nenajde program tak hleda ve shell jinou slozku
                    if (program == "") {
                        klic = Registry.CurrentUser.OpenSubKey(nazev + "\\shell\\");

                        try {
                            foreach (string hodnota in klic.GetSubKeyNames()) {
                                klic = Registry.CurrentUser.OpenSubKey(nazev + "\\shell\\" + hodnota + "\\command\\");

                                if (klic != null) {
                                    program = klic.GetValue(null,"").ToString();
                                    break;
                                }
                            }
                        }
                        catch { }
                    }

                }

                else {//jinak hledat v HKCR
                    klic = Registry.ClassesRoot.OpenSubKey(nazev);
                    if (klic != null) {//je vytvorena asociace v HKCR?
                        typ = klic.GetValue(null,"").ToString();

                        if (misto == "") misto = "CR";

                        klic = Registry.ClassesRoot.OpenSubKey(nazev + "\\DefaultIcon\\");
                        if (klic != null) ikona = klic.GetValue(null,"").ToString();

                        klic = Registry.ClassesRoot.OpenSubKey(nazev + "\\shell\\open\\command\\");
                        if (klic != null) program = klic.GetValue(null,"").ToString();

                        //pokud nenajde program tak hleda ve shell jinou slozku (podobně jak v HKCU)
                        if (program == "") {
                            klic = Registry.ClassesRoot.OpenSubKey(nazev + "\\shell\\");
                            try {
                                foreach (string hodnota in klic.GetSubKeyNames()) {
                                    klic = Registry.ClassesRoot.OpenSubKey(nazev + "\\shell\\" + hodnota + "\\command\\");

                                    if (klic != null) {
                                        program = klic.GetValue(null,"").ToString();
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            if (klic != null) klic.Close();

            SInfo inf = new SInfo();
            string[] rozdelitP = rozdelitProgram(program);
            string[] rozdelitI = rozdelitIkona(ikona);
            inf.program = rozdelitP[0];
            inf.parametr = rozdelitP[1];
            inf.ikona = rozdelitI[0];
            inf.poradi = ynt(rozdelitI[1]);
            inf.typ = typ;
            inf.nazev = nazev;
            inf.misto = misto;
            return inf;
        }

        public static bool Uloz(string pripona,string program,string parametr,string ikona,string index,string typ) {
            if (index == "") index = "0";

            string nazev = nazevPr;
            string hl_cesta1 = "Software\\Classes\\" + nazev + pripona;

            string hl_cesta2 = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + pripona;

            Microsoft.Win32.RegistryKey klic;

            try {
                //asociace do UserChoice			
                try { Registry.CurrentUser.DeleteSubKey(hl_cesta2 + "\\UserChoice\\"); }
                catch (Exception e) {
                    if (e is System.UnauthorizedAccessException) {
                        MessageBox.Show(e.ToString());
                        return false;
                    }
                }
                klic = Registry.CurrentUser.CreateSubKey(hl_cesta2 + "\\UserChoice\\");
                klic.SetValue("Progid",nazev + pripona);

                /*				
				//asociace do OpenWithProgids
				try{Registry.CurrentUser.DeleteSubKey(hl_cesta2 + "\\OpenWithProgids\\");}
				catch(Exception e){
					MessageBox.Show(e.ToString());
					return false;
				}						
				klic = Registry.CurrentUser.CreateSubKey(hl_cesta2 + "\\OpenWithProgids\\");
				klic.SetValue(nazev + pripona, new byte[0], RegistryValueKind.None);
				*/

                //nazev asociace
                klic = Registry.CurrentUser.CreateSubKey(hl_cesta1);
                klic.SetValue(null,typ);

                //ikona souboru
                if (ikona != "") {
                    klic = Registry.CurrentUser.CreateSubKey(hl_cesta1 + "\\DefaultIcon\\");
                    klic.SetValue(null,ikona + "," + index);
                }

                //uplna cesta k programu			
                klic = Registry.CurrentUser.CreateSubKey(hl_cesta1 + "\\shell\\open\\command\\");
                klic.SetValue(null,program + " " + parametr);

                //nazev asociace
                klic = Registry.CurrentUser.CreateSubKey("Software\\Classes\\." + pripona);
                klic.SetValue(null,nazev + pripona);

                //pridani programu do seznamu pruzkumniku (na 1 pozici)
                klic = Registry.CurrentUser.CreateSubKey(hl_cesta2 + "\\OpenWithList\\");
                klic.SetValue("a",System.IO.Path.GetFileName(program.Replace("\"","")));
                klic.SetValue("MRUList","a");

                klic.Close();
            }
            catch (Exception e) {
                MessageBox.Show(e.Message.ToString());
                return false;
            }
            return true;
        }

        public static bool Vymazat(string pr) {
            string nazev = nazevPr;
            string hlcesta = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\." + pr;

            try { Registry.CurrentUser.DeleteSubKeyTree(hlcesta); }
            catch {
                try {//(pokud nejde vymazat slozka s .priponou, tak vymazat aspon podslozky
                    Registry.CurrentUser.DeleteSubKey(hlcesta + "\\UserChoice\\");
                    Registry.CurrentUser.DeleteSubKey(hlcesta + "\\OpenWithProgids\\");
                }
                catch (Exception e) {
                    if (e is System.UnauthorizedAccessException)
                        return false;
                }
            }

            try {
                Registry.CurrentUser.DeleteSubKey("Software\\Classes\\." + pr);
                Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\" + nazev + pr);
            }
            catch { return false; }

            return true;
        }

        private static string[] rozdelitProgram(string program) {
            string[] vysledek = { "","" };
            string parametr;

            //najit retezec v "zavorkach"
            Match najdi = Regex.Match(program,"(^\"[\\s\\S]*?\")");
            if (najdi.Success) {
                parametr = program.Remove(0,najdi.Groups[0].Value.Length).TrimStart();
                program = najdi.Groups[0].Value;
            }
            else {//jinak rozdelit mezerou
                string[] rozdel = program.Split(' ');
                parametr = program.Remove(0,rozdel[0].Length).TrimStart();
                program = rozdel[0];
            }
            vysledek[0] = program;
            vysledek[1] = parametr;

            return vysledek;
        }

        private static string[] rozdelitIkona(string ikona) {
            string[] vysledek = { "","" };
            string poradi = "0";

            //vyhleda cislo ikony v ceste, napr. 5 nebo -123
            Match najdi = Regex.Match(ikona,"-*(\\d+)$");
            if (najdi.Success) {
                poradi = najdi.Groups[0].Value;
                ikona = ikona.Remove(ikona.Length - najdi.Groups[0].Value.Length - 1,
                                     najdi.Groups[0].Value.Length + 1).Replace("\"","");
            }
            else {
                ikona = ikona.Replace("\"","");
                poradi = "0";
            }

            vysledek[0] = ikona;
            vysledek[1] = poradi;

            return vysledek;
        }

        private static string[] rozdelitPripony(string vstup) {
            string[] pripony;
            pripony = Regex.Replace(vstup,@"\s+","").Split(',');
            if (pripony.Length == 1) pripony = (Regex.Replace(vstup,@"\s+"," ").Trim()).Split(' ');
            return pripony;
        }

        private static int ynt(string text) {
            int cislo = -100000;
            try { cislo = Int32.Parse(text); } catch { }
            return cislo;
        }

    }
}
