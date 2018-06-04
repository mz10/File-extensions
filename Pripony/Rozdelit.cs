using System.Text.RegularExpressions;

namespace Pripony {
    //rozdeleni cesty k souboru na casti - parametr programu a cislo ikony
    class Rozdelit {
        public static string[] Program(string program) {
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

        public static string[] Ikona(string ikona) {
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

        public static string[] Pripony(string vstup) {
            string[] pripony;
            pripony = Regex.Replace(vstup,@"\s+","").Split(',');
            if (pripony.Length == 1) pripony = (Regex.Replace(vstup,@"\s+"," ").Trim()).Split(' ');
            return pripony;
        }
    }
}
