/*
AWB Profiles
Copyright (C) 2007 Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualBasic.Devices;

namespace WikiFunctions.AWBProfiles
{
    struct Profile
    {
        public int id;
        public string username, password, defaultsettings, notes;
    }

    class AWBProfile
    {
        private const string RegKey = "Software\\AutoWikiBrowser\\Profiles";
        private const string PassPhrase = "oi frjweopi 4r390%^($%%^$HJKJNMHJGY 2`';'[#";
        private const string Salt = "SH1ew yuhn gxe$�$%^y HNKLHWEQ JEW`b";
        private const string IV16Chars = "tnf47bgfdwlp9,.q";

        protected string mUsername;
        private string mPassword; // or store in RAM encrypted

        Computer myComputer = new Computer();

        public string Username
        {
            //get { }
            set { }
        }

        public string Password
        {
            set { }
        }

        public string Encrypt(string text)
        {
            return Encryption.RijndaelSimple.Encrypt(text, PassPhrase, Salt, "SHA1", 2, IV16Chars, 256);
        }

        public string Decrypt(string text)
        {
            return Encryption.RijndaelSimple.Decrypt(text, PassPhrase, Salt, "SHA1", 2, IV16Chars, 256);
        }

        public List<Profile> GetProfiles()
        {
            List<Profile> profiles = new List<Profile>();

            int upper = CountSubKeys();

            for (int i = 1; i <= upper; i++)
            {
                try
                {
                    Profile prof = new Profile();
                    prof.id = i;
                    prof.username = Decrypt(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + i, "User", "").ToString());
                    prof.password = Decrypt(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + i, "Pass", "").ToString());
                    prof.defaultsettings = myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + i, "Settings", "").ToString();
                    prof.notes = myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + i, "Notes", "").ToString();

                    profiles.Add(prof);
                }
                catch { }
            }

            return profiles;
        }

        public string GetPassword(int id)
        {
            return Decrypt(myComputer.Registry.GetValue("HKEY_CURRENT_USER\\" + RegKey + "\\" + id, "Pass", "").ToString());
        }

        public void SaveProfile(Profile profile)
        {
            Microsoft.Win32.RegistryKey key = myComputer.Registry.CurrentUser.CreateSubKey(RegKey + "\\" + (CountSubKeys() + 1));

            key.SetValue("User", Encrypt(profile.username));
            if (profile.password != "")
                key.SetValue("Pass", Encrypt(profile.password));
            else
                key.SetValue("Pass", "");
            key.SetValue("Settings", profile.defaultsettings);
            key.SetValue("Notes", profile.notes);
        }

        private int CountSubKeys()
        {
            Microsoft.Win32.RegistryKey baseRegistryKey = myComputer.Registry.CurrentUser;
            Microsoft.Win32.RegistryKey key2 = baseRegistryKey.OpenSubKey(RegKey);

            return key2.SubKeyCount;
        }
    }
}
