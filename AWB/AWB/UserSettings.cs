/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using WikiFunctions;
using WikiFunctions.Plugin;
using WikiFunctions.AWBSettings;
using AutoWikiBrowser.Plugins;

namespace AutoWikiBrowser
{
    partial class MainForm
    {
        private void saveAsDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to save these settings as the default settings?", "Save as default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                SavePrefs();
        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SettingsFile))
            {
                if ((!System.IO.File.Exists(SettingsFile)) || MessageBox.Show("Replace existing file?", "File exists - " + SettingsFile,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    //Make an "old"/backup copy of a file. Old settings are still there if something goes wrong
                    File.Copy(SettingsFile, SettingsFile + ".old", true);
                    SavePrefs(SettingsFile);
                }
            }
            else if (MessageBox.Show("No settings file currently loaded. Save as Default?", "Save current settings as Default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                SavePrefs();
            else
            {
                saveCurrentSettingsToolStripMenuItem_Click(null, null);
            }
        }        

        private void loadSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadSettingsDialog();
        }

        private void loadDefaultSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Would you really like to load the original default settings?", "Reset settings to default?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ResetSettings();
        }

        private void saveCurrentSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveXML.FileName = SettingsFile;
            if (saveXML.ShowDialog() != DialogResult.OK)
                return;

            SavePrefs(saveXML.FileName);
            SettingsFile = saveXML.FileName;
        }

       /// <summary>
       /// Resets settings to Setting Class defaults
       /// </summary>
        private void ResetSettings()
        {
            try
            {
                LoadPrefs(new UserPrefs());
                LoadDefaultEditSummaries();

                try
                {
                    foreach (KeyValuePair<string, IAWBPlugin> a in Plugin.Items)
                        a.Value.Reset();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem reseting plugin\r\n\r\n" + ex.Message);
                }

                cModule.ModuleEnabled = false;
                this.Text = "AutoWikiBrowser";
                lblStatusText.Text = "Default settings loaded.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDefaultEditSummaries()
        {
            //cmboEditSummary.Items.Clear();
            cmboEditSummary.Items.Add("clean up");
            cmboEditSummary.Items.Add("re-categorisation per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("clean up and re-categorisation per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("removing category per [[WP:CFD|CFD]]");
            cmboEditSummary.Items.Add("[[Wikipedia:Template substitution|subst:'ing]]");
            cmboEditSummary.Items.Add("[[Wikipedia:WikiProject Stub sorting|stub sorting]]");
            cmboEditSummary.Items.Add("[[WP:AWB/T|Typo fixing]]");
            cmboEditSummary.Items.Add("bad link repair");
            cmboEditSummary.Items.Add("Fixing [[Wikipedia:Disambiguation pages with links|links to disambiguation pages]]");
            cmboEditSummary.Items.Add("Unicodifying");
        }

        private void LoadSettingsDialog()
        {
            if (openXML.ShowDialog() != DialogResult.OK)
                return;

            LoadPrefs(openXML.FileName);
            SettingsFile = openXML.FileName;

            listMaker1.removeListDuplicates();
        }

        private void LoadRecentSettingsList()
        {
            string s;

            splash.SetProgress(89);
            try
            {
                Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    OpenSubKey("Software\\Wikipedia\\AutoWikiBrowser");

                s = reg.GetValue("RecentList", "").ToString();
            }
            catch { return; }
            UpdateRecentList(s.Split('|'));
            splash.SetProgress(94);
        }

        public void UpdateRecentList(string[] list)
        {
            RecentList.Clear();
            RecentList.AddRange(list);
            UpdateRecentSettingsMenu();
        }

        public void UpdateRecentList(string s)
        {
            int i = RecentList.IndexOf(s);

            if (i >= 0) RecentList.RemoveAt(i);

            RecentList.Insert(0, s);
            UpdateRecentSettingsMenu();
        }

        private void UpdateRecentSettingsMenu()
        {
            while (RecentList.Count > 5)
                RecentList.RemoveAt(5);

            recentToolStripMenuItem.DropDown.Items.Clear();
            int i = 1;
            foreach (string filename in RecentList)
            {
                if (i != RecentList.Count)
                {
                    i++;
                    ToolStripItem item = recentToolStripMenuItem.DropDownItems.Add(filename);
                    item.Click += RecentSettingsClick;
                }
            }
        }

        public void SaveRecentSettingsList()
        {
            Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.
                    CreateSubKey("Software\\Wikipedia\\AutoWikiBrowser");

            StringBuilder builder = new StringBuilder();
            foreach (string s in RecentList)
            {
                if (!string.IsNullOrEmpty(s))
                    builder.Append(s + "|");
            }
            string str = builder.ToString();
            reg.SetValue("RecentList", str.Substring(0, (str.Length - 1)));
        }

        private void RecentSettingsClick(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            LoadPrefs(item.Text);
            SettingsFile = item.Text;
            listMaker1.removeListDuplicates();
        }

        /// <summary>
        /// Save preferences as default
        /// </summary>
        private void SavePrefs()
        {
            SavePrefs("Default.xml");
        }

        /// <summary>
        /// Save preferences to file
        /// </summary>
        private void SavePrefs(string path)
        {
            try
            {
                UserPrefs.SavePrefs(MakePrefs(), path);

                UpdateRecentList(path);
                SettingsFile = path;

                //Delete temporary/old file if exists when code reaches here
                if (File.Exists(SettingsFile + ".old"))
                    File.Delete(SettingsFile + ".old");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error saving settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Make preferences object from current settings
        /// </summary>
        private UserPrefs MakePrefs()
        {
             return new UserPrefs(new FaRPrefs(chkFindandReplace.Checked, findAndReplace, replaceSpecial,
                substTemplates.TemplateList, substTemplates.ExpandRecursively, substTemplates.IgnoreUnformatted,
                substTemplates.IncludeComment), new EditPrefs(chkGeneralFixes.Checked, chkAutoTagger.Checked,
                chkUnicodifyWhole.Checked, cmboCategorise.SelectedIndex, txtNewCategory.Text,
                txtNewCategory2.Text, cmboImages.SelectedIndex, txtImageReplace.Text, txtImageWith.Text,
                chkSkipNoCatChange.Checked, chkSkipNoImgChange.Checked, chkAppend.Checked, !rdoPrepend.Checked,
                txtAppendMessage.Text, (int)udNewlineChars.Value, (int)nudBotSpeed.Value, chkQuickSave.Checked, chkSuppressTag.Checked,
                chkRegExTypo.Checked), new ListPrefs(listMaker1, SaveArticleList),
                new SkipPrefs(chkSkipNonExistent.Checked, chkSkipExistent.Checked, chkSkipNoChanges.Checked, chkSkipSpamFilter.Checked,
                chkSkipIfInuse.Checked, chkSkipIfContains.Checked, chkSkipIfNotContains.Checked, txtSkipIfContains.Text,
                txtSkipIfNotContains.Text, chkSkipIsRegex.Checked, chkSkipCaseSensitive.Checked,
                chkSkipWhenNoFAR.Checked, chkSkipIfNoRegexTypo.Checked, chkSkipNoDab.Checked, chkSkipWhitespace.Checked, Skip.SelectedItem),
                new GeneralPrefs(SaveArticleList, ignoreNoBotsToolStripMenuItem.Checked, cmboEditSummary.Items,
                cmboEditSummary.Text, new string[10] {PasteMore1.Text, PasteMore2.Text, PasteMore3.Text, 
                PasteMore4.Text, PasteMore5.Text, PasteMore6.Text, PasteMore7.Text, PasteMore8.Text,
                PasteMore9.Text, PasteMore10.Text}, txtFind.Text, chkFindRegex.Checked,
                chkFindCaseSensitive.Checked, wordWrapToolStripMenuItem1.Checked, EnableToolBar,
                bypassRedirectsToolStripMenuItem.Checked, doNotAutomaticallyDoAnythingToolStripMenuItem.Checked,
                toolStripComboOnLoad.SelectedIndex, chkMinor.Checked, addAllToWatchlistToolStripMenuItem.Checked,
                showTimerToolStripMenuItem.Checked, sortAlphabeticallyToolStripMenuItem.Checked,
                addIgnoredToLogFileToolStripMenuItem.Checked, (int)txtEdit.Font.Size, txtEdit.Font.Name,
                LowThreadPriority, Beep, Flash, Minimize, TimeOut, AutoSaveEditBoxEnabled, AutoSaveEditBoxPeriod,
                AutoSaveEditBoxFile, chkLock.Checked, EditToolBarVisible, SupressUsingAWB, filterOutNonMainSpaceToolStripMenuItem.Checked,
                alphaSortInterwikiLinksToolStripMenuItem.Checked,replaceReferenceTagsToolStripMenuItem.Checked), new DabPrefs(chkEnableDab.Checked,
                txtDabLink.Text, txtDabVariants.Lines, (int)udContextChars.Value), new ModulePrefs(
                cModule.ModuleEnabled, cModule.Language, cModule.Code), externalProgram.Settings, loggingSettings1.SerialisableSettings, 
                Plugin.Items);
        }

        /// <summary>
        /// Load default preferences
        /// </summary>
        private void LoadPrefs()
        {
            splash.SetProgress(80);

            if (File.Exists("Default.xml"))
                SettingsFile = "Default.xml";

            if (!string.IsNullOrEmpty(SettingsFile))
                LoadPrefs(SettingsFile);
            else
                LoadPrefs(new UserPrefs());

            splash.SetProgress(85);
        }

        /// <summary>
        /// Load preferences from file
        /// </summary>
        private void LoadPrefs(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                findAndReplace.Clear();
                replaceSpecial.Clear();
                substTemplates.Clear();

                LoadPrefs(UserPrefs.LoadPrefs(path));

                SettingsFile = path;
                lblStatusText.Text = "Settings successfully loaded";
                UpdateRecentList(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error loading settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load preferences object
        /// </summary>
        private void LoadPrefs(UserPrefs p)
        {
            SetProject(p.LanguageCode, p.Project, p.CustomProject);

            chkFindandReplace.Checked = p.FindAndReplace.Enabled;
            findAndReplace.ignoreLinks = p.FindAndReplace.IgnoreSomeText;
            findAndReplace.ignoreMore = p.FindAndReplace.IgnoreMoreText;
            findAndReplace.AppendToSummary = p.FindAndReplace.AppendSummary;
            findAndReplace.AfterOtherFixes = p.FindAndReplace.AfterOtherFixes;
            findAndReplace.AddNew(p.FindAndReplace.Replacements);
            replaceSpecial.AddNewRule(p.FindAndReplace.AdvancedReps);

            substTemplates.TemplateList = p.FindAndReplace.SubstTemplates;
            substTemplates.ExpandRecursively = p.FindAndReplace.ExpandRecursively;
            substTemplates.IgnoreUnformatted = p.FindAndReplace.IgnoreUnformatted;
            substTemplates.IncludeComment = p.FindAndReplace.IncludeComments;

            findAndReplace.MakeList();

            listMaker1.SourceText = p.List.ListSource;
            listMaker1.SelectedSource = p.List.Source;

            SaveArticleList = p.General.SaveArticleList;

            ignoreNoBotsToolStripMenuItem.Checked = p.General.IgnoreNoBots;

            listMaker1.Add(p.List.ArticleList);
            
            chkGeneralFixes.Checked = p.Editprefs.GeneralFixes;
            chkAutoTagger.Checked = p.Editprefs.Tagger;
            chkUnicodifyWhole.Checked = p.Editprefs.Unicodify;

            cmboCategorise.SelectedIndex = p.Editprefs.Recategorisation;
            txtNewCategory.Text = p.Editprefs.NewCategory;
            txtNewCategory2.Text = p.Editprefs.NewCategory2;
            
            cmboImages.SelectedIndex = p.Editprefs.ReImage;
            txtImageReplace.Text = p.Editprefs.ImageFind;
            txtImageWith.Text = p.Editprefs.Replace;

            chkSkipNoCatChange.Checked = p.Editprefs.SkipIfNoCatChange;
            chkSkipNoImgChange.Checked = p.Editprefs.SkipIfNoImgChange;

            chkAppend.Checked = p.Editprefs.AppendText;
            rdoAppend.Checked = p.Editprefs.Append;
            rdoPrepend.Checked = !p.Editprefs.Append;
            txtAppendMessage.Text = p.Editprefs.Text;
            udNewlineChars.Value = p.Editprefs.Newlines;

            nudBotSpeed.Value = p.Editprefs.AutoDelay;
            chkQuickSave.Checked = p.Editprefs.QuickSave;
            chkSuppressTag.Checked = p.Editprefs.SuppressTag;

            chkRegExTypo.Checked = p.Editprefs.RegexTypoFix;
            
            chkSkipNonExistent.Checked = p.SkipOptions.SkipNonexistent;
            chkSkipExistent.Checked = p.SkipOptions.Skipexistent;
            chkSkipNoChanges.Checked = p.SkipOptions.SkipWhenNoChanges;
            chkSkipSpamFilter.Checked = p.SkipOptions.SkipSpamFilterBlocked;
            chkSkipIfInuse.Checked = p.SkipOptions.SkipInuse;
            chkSkipWhitespace.Checked = p.SkipOptions.SkipWhenOnlyWhitespaceChanged;

            chkSkipIfContains.Checked = p.SkipOptions.SkipDoes;
            chkSkipIfNotContains.Checked = p.SkipOptions.SkipDoesNot;

            txtSkipIfContains.Text = p.SkipOptions.SkipDoesText;
            txtSkipIfNotContains.Text = p.SkipOptions.SkipDoesNotText;

            chkSkipIsRegex.Checked = p.SkipOptions.Regex;
            chkSkipCaseSensitive.Checked = p.SkipOptions.CaseSensitive;

            chkSkipWhenNoFAR.Checked = p.SkipOptions.SkipNoFindAndReplace;
            chkSkipIfNoRegexTypo.Checked = p.SkipOptions.SkipNoRegexTypoFix;
            Skip.SelectedItem = p.SkipOptions.GeneralSkip;
            chkSkipNoDab.Checked = p.SkipOptions.SkipNoDisambiguation;

            cmboEditSummary.Items.Clear();

            if (p.General.Summaries.Count == 0)
                LoadDefaultEditSummaries();
            else
                foreach (string s in p.General.Summaries)
                    cmboEditSummary.Items.Add(s);

            chkLock.Checked = p.General.LockSummary;
            EditToolBarVisible = p.General.EditToolbarEnabled;

            cmboEditSummary.Text = p.General.SelectedSummary;

            if (chkLock.Checked)
                lblSummary.Text = p.General.SelectedSummary;

            PasteMore1.Text = p.General.PasteMore[0];
            PasteMore2.Text = p.General.PasteMore[1];
            PasteMore3.Text = p.General.PasteMore[2];
            PasteMore4.Text = p.General.PasteMore[3];
            PasteMore5.Text = p.General.PasteMore[4];
            PasteMore6.Text = p.General.PasteMore[5];
            PasteMore7.Text = p.General.PasteMore[6];
            PasteMore8.Text = p.General.PasteMore[7];
            PasteMore9.Text = p.General.PasteMore[8];
            PasteMore10.Text = p.General.PasteMore[9];

            txtFind.Text = p.General.FindText;
            chkFindRegex.Checked = p.General.FindRegex;
            chkFindCaseSensitive.Checked = p.General.FindCaseSensitive;

            wordWrapToolStripMenuItem1.Checked = p.General.WordWrap;
            EnableToolBar = p.General.ToolBarEnabled;
            bypassRedirectsToolStripMenuItem.Checked = p.General.BypassRedirect;
            doNotAutomaticallyDoAnythingToolStripMenuItem.Checked = p.General.NoAutoChanges;
            toolStripComboOnLoad.SelectedIndex = p.General.OnLoadAction;
            chkMinor.Checked = p.General.Minor;
            addAllToWatchlistToolStripMenuItem.Checked = p.General.Watch;
            showTimerToolStripMenuItem.Checked = p.General.TimerEnabled;
            ShowTimer();

            sortAlphabeticallyToolStripMenuItem.Checked = p.General.SortListAlphabetically;

            addIgnoredToLogFileToolStripMenuItem.Checked = p.General.AddIgnoredToLog;

            AutoSaveEditBoxEnabled = p.General.AutoSaveEdit.Enabled;
            AutoSaveEditBoxPeriod = p.General.AutoSaveEdit.SavePeriod;
            AutoSaveEditBoxFile = p.General.AutoSaveEdit.SaveFile;

            SupressUsingAWB = p.General.SupressUsingAWB;

            filterOutNonMainSpaceToolStripMenuItem.Checked = p.General.filterNonMainSpace;

            alphaSortInterwikiLinksToolStripMenuItem.Checked = p.General.SortInterWikiOrder;
            replaceReferenceTagsToolStripMenuItem.Checked = p.General.ReplaceReferenceTags;

            txtEdit.Font = new System.Drawing.Font(p.General.TextBoxFont, p.General.TextBoxSize);

            LowThreadPriority = p.General.LowThreadPriority;
            Flash = p.General.Flash;
            Beep = p.General.Beep;

            Minimize = p.General.Minimize;
            TimeOut = p.General.TimeOutLimit;
            webBrowserEdit.TimeoutLimit = int.Parse(TimeOut.ToString());
            
            chkEnableDab.Checked = p.Disambiguation.Enabled;
            txtDabLink.Text = p.Disambiguation.Link;
            txtDabVariants.Lines = p.Disambiguation.Variants;
            udContextChars.Value = p.Disambiguation.ContextChars;

            loggingSettings1.SerialisableSettings = p.Logging;

            cModule.ModuleEnabled = p.Module.Enabled;
            cModule.Language = p.Module.Language;
            cModule.Code = p.Module.Code.Replace("\n", "\r\n");
            if (cModule.ModuleEnabled) cModule.MakeModule();

            externalProgram.Settings = p.ExternalProgram;

            foreach (PluginPrefs pp in p.Plugin)
            {
                if (Plugin.Items.ContainsKey(pp.Name))
                    Plugin.Items[pp.Name].LoadSettings(pp.PluginSettings);
            }
        }
    }
}
