using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Pripony{
    //Ikony - dialog
	public partial class Ikony : Window{
		int index = 0;
		
		public Ikony(string umisteni, int ind){
			InitializeComponent();
			index = ind;
			textBox1.Text = umisteni;
			tbindex.Text = ind.ToString();
			zobraz();
		}	
				
	#region winapi
		[DllImport("shell32.dll")]
		static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, StringBuilder lpIconPath, out ushort lpiIcon);
		
		[DllImport("shell32.dll")]
		static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll",EntryPoint = "ExtractIconEx")]
        private static extern int ExtractIconExA(string lpszFile,int nIconIndex,ref IntPtr phiconLarge,ref IntPtr phiconSmall,int nIcons);

        [DllImport("user32")]
        private static extern int DestroyIcon(IntPtr hIcon);
        #endregion

        void umisteni() {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Multiselect = false;		
			ofd.Title = "Vyber ikonu...";
			ofd.InitialDirectory = "previousPath";
			ofd.Filter = "Soubory ikon|*.ico;*.dll;*.exe;*.cpl;*.ani;*.cur|Ikony v DLL|*.dll|" +
						 "Ikony v EXE|*.exe|ICO|*.ico|Ostatní|*.cpl;*.ani;*.cur";
			ofd.ValidateNames = true;
			
			if (ofd.ShowDialog() == true) {
				textBox1.Text = ofd.FileName;
				zobraz();
			}
		}		
	
		void potvrd() {
			this.DialogResult = true;
		}
	
		void zobraz() {
			if (!File.Exists(textBox1.Text)) umisteni();
			listView1.Focus();

			StringBuilder sbcesta = new StringBuilder(textBox1.Text);
			Icon ikona = null;
			int pocet = (int)ExtractIcon(IntPtr.Zero, textBox1.Text, -1) - 1;
			
			try {
				listView1.Items.Clear();
				
				for (int i=0;i<=pocet;i++) {
					ushort ind = (ushort)i;
					
					ikona = System.Drawing.Icon.FromHandle(ExtractAssociatedIcon(IntPtr.Zero, sbcesta, out ind));
			
	          		var obrazek = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
	                                  ikona.Handle,
	                                  System.Windows.Int32Rect.Empty,
	                                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());  
					
					
					
					listView1.Items.Add(new polozky{lwobrazek = obrazek, lwjmeno = i.ToString()});
					
				}
			} catch{}
		}	
	
		public string cesta {
			get {return textBox1.Text;}
        }
	
		public string cislo {
			get {return tbindex.Text;}
        }

        public static ImageSource Otevrit(string path,int poradi,int velikost) {
            IntPtr velkaI = IntPtr.Zero;
            IntPtr malaI = IntPtr.Zero;

            ExtractIconExA(path.Replace("\"",""),poradi,ref velkaI,ref malaI,1);

            Icon ikona = null;
            if (malaI != IntPtr.Zero) {
                if (velikost == 0) {
                    ikona = System.Drawing.Icon.FromHandle(malaI);
                    DestroyIcon(velkaI);
                }
                if (velikost == 1) {
                    ikona = System.Drawing.Icon.FromHandle(velkaI);
                    DestroyIcon(malaI);
                }

                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                        ikona.Handle,
                                        System.Windows.Int32Rect.Empty,
                                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            return null;
        }

        public struct polozky {
      		public ImageSource lwobrazek {get; set;}
	        public string lwjmeno {get; set;}
   		}		
	
		void totevrit_Click(object sender, RoutedEventArgs e){
			umisteni();
		}
		
		void tok_Click(object sender, RoutedEventArgs e){
			zobraz();
		}
				
		void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e){
			var polozka = (polozky)listView1.SelectedItem;
			tbindex.Text = polozka.lwjmeno;
		}
		
		void listView1_KeyUp(object sender, KeyEventArgs e){
			if (e.Key == Key.Enter) potvrd();
		}
		
		void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e){
			potvrd();
		}
		
		void textBox1_KeyUp(object sender, KeyEventArgs e){
			if (e.Key == Key.Enter) zobraz();
		}
	
		void window1_Drop(object sender, DragEventArgs e){
			string[] soubory = (string[])e.Data.GetData(DataFormats.FileDrop, true);
			textBox1.Text = soubory[0];
			zobraz();
		}
	}
}