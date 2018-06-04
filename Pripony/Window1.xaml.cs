using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

//using IWshRuntimeLibrary; //script host - lnk

namespace Pripony {
    public partial class Window1 : Window {
        #region promenne
        public Window1() {
            InitializeComponent();
            string[] soubory = Environment.GetCommandLineArgs();
            if (soubory.Length > 1 && File.Exists(soubory[1]))
                CSVImport(soubory[1]);
            else seznampripon();
        }

        /* ZKRATKY V MENU:
         zobrazit upravene pripony
         skryt prazne pripony
         zobrazit vsechno 
        */
        public bool ZUP = false;
        public bool SPP = true;
        public bool ZV = true;
        public bool zatrhnout = false;

        #endregion

        #region funkce	
        public void seznampripon() {
            listView1.Items.Clear();
            tobnovit.IsEnabled = true;
            zatrhnout = false;
            chb_listview.IsChecked = false;
            tZmenaLV.Content = "Obnovit";

            Task ukol = new Task(() => {
                foreach (string klic in Registry.ClassesRoot.GetSubKeyNames()) {
                    if (klic.Substring(0,1) == ".") {
                        string pripona = klic.Replace(".","");
                        string cesta;
                        int index;

                        var info = ASC.Nacti(pripona);

                        if (SPP && info.typ == "" && info.program == "" && info.ikona == "") {
                            continue;
                        }

                        if (ZUP) {
                            if (!(info.nazev.Length >= 5 && info.nazev.Substring(0,ASC.nazevPr.Length) == ASC.nazevPr))
                                continue;
                        }

                        if (info.ikona == "") {
                            cesta = info.program.Replace("\"","");
                            index = 0;
                        }
                        else {
                            cesta = info.ikona;
                            index = info.poradi;
                        }

                        string cesta_ikona = "";
                        ImageSource obraz;

                        if (info.ikona != "") {
                            cesta_ikona = info.ikona + "," + info.poradi;
                            obraz = Ikony.Otevrit(info.ikona,info.poradi,0);
                        }
                        else obraz = Ikony.Otevrit(info.program,0,0);

                        try { obraz.Freeze(); } catch { }

                        Dispatcher.BeginInvoke(DispatcherPriority.Background,(ThreadStart)delegate () {
                            listView1.Items.Add(
                                new Polozky {
                                    lvzaskrknuto = zatrhnout,
                                    lvobrazek = obraz,
                                    lvpripona = pripona,
                                    lvtyp = info.typ,
                                    lvprogram = info.program + " " + info.parametr,
                                    lvikona = cesta_ikona
                                });
                        });
                    }
                }
            });
            ukol.Start();
        }

        public void CSVExport(bool vsechno) {
            SaveFileDialog sd = new SaveFileDialog();
            sd.Title = "Uložit soubor...";
            sd.InitialDirectory = "previousPath";
            sd.Filter = "CSV|*.csv|TXT|*.txt";
            sd.ValidateNames = true;

            if (sd.ShowDialog() == true) {
                using (StreamWriter sw = new StreamWriter(sd.FileName,true,Encoding.GetEncoding(1250))) {
                    foreach (var radek in listView1.Items) {
                        var sloupec = (Polozky)radek;

                        if (sloupec.lvzaskrknuto || vsechno) {
                            sw.WriteLine(
                                sloupec.lvpripona + ";" +
                                sloupec.lvtyp + ";" +
                                sloupec.lvprogram + ";" +
                                sloupec.lvikona
                            );
                        }
                    }
                    sw.Flush();
                }
            }
        }

        public void CSVImport(string cesta) {
            OpenFileDialog od = null;

            if (cesta == "") {
                od = new OpenFileDialog();
                od.Title = "Otevřít soubor...";
                od.InitialDirectory = "previousPath";
                od.Filter = "CSV|*.csv|TXT|*.txt";
                od.ValidateNames = true;
            }

            if (cesta != "" || (od.ShowDialog() == true)) {
                ZUP = false;
                ZV = false;
                menuZUP.IsChecked = false;
                menuZV.IsChecked = false;
                tZmenaLV.Content = "Přidat";
                zatrhnout = true;
                chb_listview.IsChecked = true;
                listView1.Items.Clear();

                if (cesta == "") cesta = od.FileName;

                Task ukol2 = new Task(() => {
                    try {
                        using (StreamReader sr = new StreamReader(cesta,Encoding.GetEncoding(1250))) {
                            string radek;

                            while ((radek = sr.ReadLine()) != null) {
                                string[] bunka = radek.Split(';');

                                string b0, b1, b2, b3;
                                b0 = b1 = b2 = b3 = "";

                                if (bunka.Length > 0) b0 = bunka[0];
                                if (bunka.Length > 1) b1 = bunka[1];
                                if (bunka.Length > 2) b2 = bunka[2];
                                if (bunka.Length > 3) b3 = bunka[3];

                                Dispatcher.BeginInvoke(DispatcherPriority.Send,(ThreadStart)delegate () {
                                    listView1.Items.Add(
                                        new Polozky {
                                            lvzaskrknuto = zatrhnout,
                                            //lvobrazek = null,
                                            lvpripona = b0,
                                            lvtyp = b1,
                                            lvprogram = b2,
                                            lvikona = b3,
                                        });
                                });
                            }
                        }
                    }
                    catch { MessageBox.Show("Soubor se nepodařilo načíst"," ",0,MessageBoxImage.Warning); }
                });
                ukol2.Start();
            }
        }

        public int ynt(string text) {
            int cislo = -100000;
            try { cislo = Int32.Parse(text); } catch { }
            return cislo;
        }

        public void vyplnit() {
            reset();
            string[] pr = Rozdelit.Pripony(tbpripona.Text);
            string mpripona = pr[0].ToLower();

            var info = ASC.Nacti(mpripona);
            tbprogram.Text = info.program;
            tbparam.Text = info.parametr;
            tbtyp.Text = info.typ;
            tbikona.Text = info.ikona;
            tbindex.Text = info.poradi.ToString();

            //zakazane pripony pro asociaci
            bool p1 = info.misto == "UP";
            bool p2 = info.misto == "UO";
            bool p3 = mpripona != "lnk";
            bool p4 = mpripona != "exe";
            bool p5 = mpripona != "dll";
            bool p6 = mpripona != "";

            if ((p1 || p2) && p3 && p4 && p5 && p6)
                tobnovit.IsEnabled = true;
            else tobnovit.IsEnabled = false;

            if (p3 && p4 && p5 && p6)
                tbzmenit.IsEnabled = true;
            else tbzmenit.IsEnabled = false;

            nactiIkonu(info.program,info.ikona,info.poradi);
        }

        //nacte ikonu do prvku "obrazek"
        public void nactiIkonu(string program,string ikona,int index) {
            if (ikona == "") {
                try { obrazek.ImageSource = Ikony.Otevrit(program,0,1); }
                catch { obrazek.ImageSource = null; }
            }
            else {
                try { obrazek.ImageSource = Ikony.Otevrit(ikona,index,1); }
                catch { obrazek.ImageSource = null; }
            }
        }

        public void reset() {
            //nechat// pripona.Text = "";
            tbtyp.Text = "";
            tbprogram.Text = "";
            tbparam.Text = "";
            tbikona.Text = "";
            tbindex.Text = "";
            //obrazek.ImageSource = null;
        }
        #endregion

        #region udalosti
        void tobnovit_Click(object sender,RoutedEventArgs e) {
            ASC.Vymazat(tbpripona.Text);
            reset();
            vyplnit();
            seznampripon();
        }

        void tprogram_Click(object sender,RoutedEventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "Vyber program...";
            fd.InitialDirectory = "previousPath";
            fd.Filter = "Spustitelné soubory|*.dll;*.exe;*.bat;*.vbs;*.lnk|EXE|*.exe|DLL|*.dll|BAT|*.bat";
            if (fd.ShowDialog() == true) {
                /*
				if(Path.GetExtension(fd.FileName).ToLower() == ".lnk") {
					WshShell shell = new WshShell();
					IWshShortcut link = (IWshShortcut)shell.CreateShortcut(fd.FileName);
					program.Text = "\"" + link.TargetPath + "\"";
				}
				
				else program.Text = "\"" + fd.FileName + "\"";
				*/

                tbprogram.Text = "\"" + fd.FileName + "\"";
                tbparam.Text = "\"%1\"";
            }
        }

        void tbzmenit_Click(object sender,RoutedEventArgs e) {
            string[] pripony = Rozdelit.Pripony(tbpripona.Text);

            if (tbpripona.Text == "" || tbprogram.Text == "") MessageBox.Show("Nejdřív to vyplň!","",0,MessageBoxImage.Warning);
            else {
                if (pripony.Length > 1) {
                    MessageBoxResult jn = MessageBox.Show(
                        "Změní se přípon: " + Rozdelit.Pripony(tbpripona.Text).Length + "\nPokračovat?",
                        "Upozornění",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (jn == MessageBoxResult.No) return;
                }

                int i = 0;
                foreach (string prip in pripony) {
                    string mpripona = prip.ToLower();
                    if (mpripona == "dll" || mpripona == "exe" || mpripona == "lnk") MessageBox.Show("continue");

                    string typ = tbtyp.Text.Replace("§PR",prip.ToUpper()).Replace("§pr",prip.ToLower());

                    if (ASC.Uloz(prip,
                                 tbprogram.Text,
                                 tbparam.Text,
                                 tbikona.Text,
                                 tbindex.Text,
                                 typ)
                        ) i++;
                }

                if (pripony.Length > 1) {
                    MessageBox.Show(
                        "Úspěšně asociovaných přípon: " + i + "/" + pripony.Length,"",
                        0,MessageBoxImage.Information
                    );
                }

                reset();
                vyplnit();
                seznampripon();
            }
        }

        void tikona_Click(object sender,RoutedEventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Title = "Vyber ikonu...";
            fd.InitialDirectory = "previousPath";
            fd.Filter = "Soubory ikon|*.ico;*.dll;*.exe;*.cpl;*.ani;*.cur|Ikony v DLL|*.dll|" +
                         "Ikony v EXE|*.exe|ICO|*.ico|Ostatní|*.cpl;*.ani;*.cur";
            fd.ValidateNames = true;

            if (fd.ShowDialog() == true) {
                tbindex.Text = "0";
                tbikona.Text = fd.FileName;
            }
        }

        void tbpripona_TextChanged(object sender,TextChangedEventArgs e) {
            vyplnit();
        }

        void listView1_SelectionChanged(object sender,SelectionChangedEventArgs e) {
            var polozka = (Polozky)listView1.SelectedItem;
            if (polozka != null)
                tbpripona.Text = polozka.lvpripona;
            else tbpripona.Text = "";
        }

        void menuCSVE_Click(object sender,RoutedEventArgs e) {
            CSVExport(true);
        }

        void menuCSVI_Click(object sender,RoutedEventArgs e) {
            CSVImport("");
        }

        void obikona_MouseLeftButtonUp(object sender,MouseButtonEventArgs e) {
            string cesta;
            int cislo;

            if (tbikona.Text == "") {
                cesta = tbprogram.Text.Replace("\"","");
                cislo = 0;
            }
            else {
                cesta = tbikona.Text;
                cislo = ynt(tbindex.Text);
            }

            if (tbindex.Text == "") cislo = 0;

            Ikony ik = new Ikony(cesta,cislo);
            if (ik.ShowDialog() == true) {
                tbikona.Text = ik.cesta;
                tbindex.Text = ik.cislo;
            }
        }

        void tbindex_TextChanged(object sender,TextChangedEventArgs e) {
            int index;
            if (tbindex.Text == "") index = 0;
            else index = ynt(tbindex.Text);
            nactiIkonu(tbprogram.Text,tbikona.Text,index);
        }

        void tbikona_TextChanged(object sender,TextChangedEventArgs e) {
            int index;
            if (tbindex.Text == "") index = 0;
            else index = ynt(tbindex.Text);
            nactiIkonu(tbprogram.Text,tbikona.Text,index);
        }


        void ZUP_Click(object sender,RoutedEventArgs e) {
            ZV = false;
            menuZV.IsChecked = false;
            ZUP = !ZUP;
            menuZUP.IsChecked = ZUP;
            seznampripon();
        }

        void SPP_Click(object sender,RoutedEventArgs e) {
            SPP = !SPP;
            menuSPP.IsChecked = SPP;
            seznampripon();
        }

        void Konec_Click(object sender,RoutedEventArgs e) {
            Close();
        }

        void ZV_Click(object sender,RoutedEventArgs e) {
            ZUP = false;
            menuZUP.IsChecked = false;
            ZV = !ZV;
            menuZV.IsChecked = ZV;

            seznampripon();
        }

        void chb_listview_Checked(object sender,RoutedEventArgs e) {
            zatrhnout = !zatrhnout;
            chb_listview.IsChecked = zatrhnout;

            foreach (Polozky p in listView1.Items)
                p.lvzaskrknuto = zatrhnout;

            listView1.Items.Refresh();
        }

        void tExport_Click(object sender,RoutedEventArgs e) {
            CSVExport(false);
        }

        void tZmenaLV_Click(object sender,RoutedEventArgs e) {
            int i = 0;

            if (tZmenaLV.Content.ToString() == "Obnovit") {
                foreach (Polozky p in listView1.Items)
                    if (p.lvzaskrknuto) i++;

                var polozka = (Polozky)listView1.SelectedItem;
                if (polozka != null && i == 0) {
                    i = 1;
                    MessageBoxResult jn1 = MessageBox.Show(
                        "Opravdu smazat tuto příponu?: " + polozka.lvpripona,
                        "Upozornění",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (jn1 == MessageBoxResult.Yes) {
                        ASC.Vymazat(polozka.lvpripona);
                        seznampripon();
                    }
                    return;
                }

                MessageBoxResult jn2 = MessageBox.Show(
                    "Opravdu smazat tyto přípony (" + i + ")?",
                    "Upozornění",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (jn2 == MessageBoxResult.No) return;

                int y = 0;
                foreach (Polozky p in listView1.Items) {
                    if (p.lvzaskrknuto && ASC.Vymazat(p.lvpripona)) y++;
                }

                MessageBox.Show(
                    "Smazaných přípon: (" + y + "/" + i + ")",
                    "Upozornění",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else {
                foreach (Polozky p in listView1.Items) {
                    if (p.lvzaskrknuto) {
                        string[] program = Rozdelit.Program(p.lvprogram);
                        string[] ikona = Rozdelit.Ikona(p.lvikona);

                        if (ASC.Uloz(
                            p.lvpripona,
                            program[0],
                            program[1],
                            ikona[0],
                            ikona[1],
                            p.lvtyp
                        )) i++;
                    }
                }
                MessageBox.Show("Přidaných přípon: " + i,"",0,MessageBoxImage.Information);
            }
            seznampripon();
        }

        void window1_Drop(object sender,DragEventArgs e) {
            string[] soubory = (string[])e.Data.GetData(DataFormats.FileDrop,true);
            CSVImport(soubory[0]);
        }
        #endregion

    }
}