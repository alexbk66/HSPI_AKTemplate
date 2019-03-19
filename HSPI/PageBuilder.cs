// Copyright (C) 2016 SRG Technology, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;

using HomeSeerAPI;
using Scheduler;
using static Scheduler.clsJQuery;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

namespace HSPI_AKTemplate
{

    /// <summary>
    /// This class adds some common support functions for creating the web pages used by HomeSeer plugins. 
    /// <para/>For each control there are three functions:  
    /// <list type="bullet">
    /// <item><description><c>Build:</c> Used to initially create the control in the web page.</description></item>
    /// <item><description><c>Update:</c> Used to modify the control in an existing web page.</description></item>
    /// <item><description><c>Form:</c> Not normally call externally but could be useful in special circumstances.</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="Scheduler.PageBuilderAndMenu.clsPageBuilder" />
    public class PageBuilder : PageBuilderAndMenu.clsPageBuilder
    {
        /// <summary>
        /// PageBuilder Constructor.
        /// </summary>
        /// <param name="pagename">The name used by HomeSeer when referencing this particular page.</param>
        /// <param name="plugin"></param>
        /// <param name="register">Don't RegisterWebPage for "deviceutility" page</param>
        /// <param name="config">RegisterConfigLink for "manage Interfaces" page</param>
        public PageBuilder(string pagename, HspiBase2 plugin, bool register = true, bool config = false) : base(pagename)
        {
            UsesJqAll = false;

            this.plugin = plugin;
            gLocLabel = hs.GetINISetting("Settings", "gLocLabel", "");
            gLocLabel2 = hs.GetINISetting("Settings", "gLocLabel2", "");
            bUseLocation2 = "True" == hs.GetINISetting("Settings", "bUseLocation2", "");
            bLocationFirst = "True" == hs.GetINISetting("Settings", "bLocationFirst", "");
            if (bLocationFirst)
                LocationLabels = Tuple.Create(gLocLabel, gLocLabel2);
            else
                LocationLabels = Tuple.Create(gLocLabel2, gLocLabel);

            if(register || config)
                RegisterWebPage(config);
        }

        /// <summary>
        /// Register the page Link for Plugins Menu
        /// </summary>
        /// <param name="config">Also RegisterConfigLink for "manage Interfaces" page</param>
        /// <returns></returns>
        public string RegisterWebPage(bool config = false)
        {
            try
            {
                if (config)
                    plugin.RegisterWebPage(linktext: this.PageName, config: true);

                string link = plugin.RegisterWebPage(linktext: this.PageName, config: false);

                // Store for creating clsJQuery controls
                this.page_link = link;

                //PageBuilder.registerPage(page.PageName, page);
                registerPage(link, this);
                registerPage("/" + link, this);

                return link;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Registering Web Links (RegisterWebPage): " + ex.Message);
                return null;
            }
        }


        #region MemberVariables

        public const char id_prefix = '_';

        protected HspiBase2 plugin;
        public IHSApplication hs { get { return plugin.HS; } }

        // From HS configuration
        protected string gLocLabel = null;
        protected string gLocLabel2 = null;
        protected bool bUseLocation2 = false;
        public static bool bLocationFirst = false;
        public static Tuple<string, string> LocationLabels = null;

        // Webpage link created by Utils.RegisterWebPage()
        public string page_link { set; get; }

        #endregion MemberVariables

        #region virtual members

        /// <summary>
        /// Measure Duration of BuildContent() call
        /// </summary>
        StopwatchEx watch;

        /// <summary>
        /// Main functions which generates page HTML
        /// Calls virtual BuildContent function
        /// </summary>
        /// <returns></returns>
        public virtual string GetHTML(bool addHeaderFooter = false, NameValueCollection queryPairs = null)
        {
            watch = new StopwatchEx(PageName);
            reset();
            suppressDefaultFooter = true;

            string html = BuildContent(queryPairs);

            if (addHeaderFooter)
            {
                string title = plugin.Name + " " + PageName + ""; // Instance
                string header = hs.GetPageHeader(PageName, title, "", "", false, false);
                AddHeader(header);

                //////////////////////////////
                AddBody( html );
                //////////////////////////////

                AddFooter(hs.GetPageFooter());
            }

            //Utils.Log($"Duration '{PageName}': {duration} ms.");

            if (addHeaderFooter)
                return BuildPage();
            else
                return html;
        }

        public long duration { get => watch.Stop(); }


        /// <summary>
        /// Virtual function for creating page body
        /// Should be overidden by derived classes
        /// </summary>
        /// <returns></returns>
        public virtual string BuildContent(NameValueCollection queryPairs = null)
        {
            return "";
        }

        #endregion virtual members

        #region Button

        /// <summary>
        /// Build a button for a web page.
        /// </summary>
        /// <param name="Text">The text on the button.</param>
        /// <param name="Name">The name used to create the references for the button.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>The text to insert in the web page to create the button.</returns>
        protected string BuildButton(string Text, string Name, bool Enabled = true)
        {
            return "<div id='" + Name + "_div'>" + FormButton(Name, Text, Enabled: Enabled) + "</div>";
        }

        /// <summary>
        /// Update a button on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text on the button.</param>
        /// <param name="Name">The name used to create the references for the button.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        protected void UpdateButton(string Text, string Name, bool Enabled = true)
        {
            divToUpdate.Add(Name + "_div", FormButton(Name, Text, Enabled: Enabled));
        }

        /// <summary>
        /// Return the string required to create a web page button.
        /// </summary>
        protected string FormButton( string Name,
                                     string label = "Submit",
                                     bool SubmitForm = true,
                                     string ImagePathNormal = "",
                                     string ImagePathPressed = "",
                                     string ToolTip = "",
                                     bool Enabled = true,
                                     string Style = "",
                                     string className = "",
                                     string url = null,
                                     int width=0)
        {
            jqButton b = new jqButton(Name, label, PageName, SubmitForm)
            {
                id = id_prefix + Name,
                url = url,
                hyperlink = false,
                urlNewWindow = true,
                imagePathNormal = ImagePathNormal,
                toolTip = ToolTip,
                enabled = Enabled,
                style = Style,
                width = width
            };
            b.imagePathPressed = (ImagePathPressed != "") ? ImagePathPressed : b.imagePathNormal;

            string Button = b.Build();
            //Button.Replace("</button>\r\n", "</button>");

            // Replace "Submit" label
            Button = Button.Replace($"&{Name}=Submit", $"&{Name}={label}");

            return Button;
        }

        #endregion Button

        #region Label

        /// <summary>
        /// Build a label for a web page.
        /// </summary>
        /// <param name="Text">The text for the label.</param>
        /// <param name="Name">The name used to create the references for the label.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns>The text to insert in the web page to create the label.</returns>
        protected string BuildLabel(string Name, string Msg = "")
        {
            return "<div id='" + Name + "_div'>" + FormLabel(Name, Msg) + "</div>";
        }

        /// <summary>
        /// Update a label on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text for the label.</param>
        /// <param name="Name">The name used to create the references for the label.</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        protected void UpdateLabel(string Name, string Msg = "")
        {
            divToUpdate.Add(Name + "_div", FormLabel(Name, Msg));
        }

        /// <summary>
        /// Return the string required to create a web page label.
        /// </summary>
        protected string FormLabel(string Name, string Message = "", bool Visible = true)
        {
            string Content;
            if (Visible)
                Content = Message + "<input id='" + Name + "' Name='" + Name + "' Type='hidden'>";
            else
                Content = "<input id='" + Name + "' Name='" + Name + "' Type='hidden' value='" + Message + "'>";
            return Content;
        }

        #endregion Label

        #region TextBox

        protected string FormTextBox( string Name,
                                      string DefaultText = "",
                                      string dialogCaption = "",
                                      string promptText = "",
                                      int size = 0,
                                      bool SubmitForm = true,
                                      string style = "")
        {
            var txtBox = new jqTextBox(Name, "text", DefaultText, PageName, size, SubmitForm)
            {
                dialogCaption = dialogCaption,
                promptText = promptText,
                style = style
            };
            return txtBox.Build();
        }

        /// <summary>
        /// Build a text entry box for a web page.
        /// </summary>
        /// <param name="Text">The default text for the text box.</param>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="AllowEdit">if set to <c>true</c> allow the text to be edited.</param>
        /// <returns>The text to insert in the web page to create the text box.</returns>
        protected string BuildTextBox(string Name, string Text = "", bool AllowEdit = true)
        {
            return "<div id='" + Name + "_div'>" + HTMLTextBox(Name, Text, 20, AllowEdit) + "</div>";
        }

        /// <summary>
        /// Update a text box on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Text">The text for the text box.</param>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="AllowEdit">if set to <c>true</c> allow the text to be edited.</param>
        protected void UpdateTextBox(string Name, string Text = "", bool AllowEdit = true)
        {
            divToUpdate.Add(Name + "_div", HTMLTextBox(Name, Text, 20, AllowEdit));
        }

        /// <summary>
        /// Return the string required to create a web page text box.
        /// </summary>
        protected string HTMLTextBox(string Name, string DefaultText, int Size, bool AllowEdit = true)
        {
            string Style = "";
            string sReadOnly = "";

            if (!AllowEdit)
            {
                Style = "color:#F5F5F5; background-color:#C0C0C0;";
                sReadOnly = "readonly='readonly'";
            }

            return $"<input type='text' id='{id_prefix + Name}' style='{Style}' size='{Size}' name='{Name}' {sReadOnly} value='{DefaultText}'>";
        }

        #endregion TextBox

        #region CheckBox

        /// <summary>
        /// Build a check box for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="Checked">if set to <c>true</c> [checked].</param>
        /// <returns>The text to insert in the web page to create the check box.</returns>
        protected string BuildCheckBox( string Name,
                                        bool Checked = false,
                                        bool SubmitForm = true)
        {
            return "<div id='" + Name + "_div'>" + FormCheckBox(Name, Checked, SubmitForm: SubmitForm) + "</div>";
        }

        /// <summary>
        /// Update a check box on a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the text box.</param>
        /// <param name="Checked">if set to <c>true</c> [checked].</param>
        protected void UpdateCheckBox(string Name, bool Checked = false)
        {
            divToUpdate.Add(Name + "_div", FormCheckBox(Name, Checked));
        }

        /// <summary>
        /// Return the string required to create a web page check box.
        /// </summary>
        protected string FormCheckBox( string Name,
                                       bool Checked = false,
                                       string label="",
                                       bool AutoPostBack = true,
                                       bool SubmitForm = true)
        {
            UsesjqCheckBox = true;

            jqCheckBox cb = new jqCheckBox(Name, label, PageName, AutoPostBack, SubmitForm)
            {
                id = id_prefix + Name,
                @checked = Checked
            };
            return cb.Build();
        }

        #endregion CheckBox

        #region ListBox

        /// <summary>
        /// Build a list box for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <param name="Width">Width of the list box</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].  Doesn't seem to work.</param>
        /// <returns>The text to insert in the web page to create the list box.</returns>
        protected string BuildListBox(string Name, ref MyPairList Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            return "<div id='" + Name + "_div'>" + FormListBox(Name, ref Options, Selected, SelectedValue, Width, Enabled) + "</div>";
        }

        /// <summary>
        /// Update a list box for a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <param name="Width">Width of the list box</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].  Doesn't seem to work.</param>
        protected void UpdateListBox(string Name, ref MyPairList Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            divToUpdate.Add(Name + "_div", FormListBox(Name, ref Options, Selected, SelectedValue, Width, Enabled));
        }

        /// <summary>
        /// Return the string required to create a web page list box.
        /// </summary>
        protected string FormListBox(string Name, ref MyPairList Options, int Selected = -1, string SelectedValue = "", int Width = 150, bool Enabled = true)
        {
            UsesjqListBox = true;

            jqListBox lb = new jqListBox(Name, PageName)
            {
                id = id_prefix + Name,
                style = "width: " + Width + "px;",
                enabled = Enabled,
            };
            lb.items.Clear();
            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    if ((Selected == -1) && (SelectedValue == Options[i].ValueStr))
                        Selected = i;
                    lb.items.Add(Options[i].ValueStr);
                }
                if (Selected >= 0)
                    lb.SelectedValue = Options[Selected].ValueStr;
            }

            return lb.Build();
        }

        #endregion ListBox

        #region DropList

        /// <summary>
        /// Build a drop list for a web page.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        /// <returns>The text to insert in the web page to create the drop list.</returns>
        protected string BuildDropList(string Name, ref MyPairList Options, out MyPair selectedPair, int Selected = -1, string SelectedValue = "", bool AddBlankRow = false, int width = 150)
        {
            return "<div id='" + Name + "_div'>" + FormDropDown(Name, ref Options, Selected, out selectedPair,
                SelectedValue: SelectedValue, AddBlankRow: AddBlankRow, width: width) + "</div>";
        }

        /// <summary>
        /// Update a drop list for a web page that was created with a DIV tag.
        /// </summary>
        /// <param name="Name">The name used to create the references for the list box.</param>
        /// <param name="Options">Data value pairs used to populate the list box.</param>
        /// <param name="Selected">Index of the item to be selected.</param>
        /// <param name="SelectedValue">Name of the value to be selected.</param>
        protected void UpdateDropList(string Name, ref MyPairList Options, out MyPair selectedPair, int Selected = -1, string SelectedValue = "")
        {
            divToUpdate.Add(Name + "_div", FormDropDown(Name, ref Options, Selected, out selectedPair, SelectedValue: SelectedValue));
        }

        #endregion DropList

        #region DropDown

        /// <summary>
        /// Return the string required to create a web page drop list.
        /// </summary>
        protected string FormDropDown( string Name,
                                       ref MyPairList Options,
                                       int selected,
                                       out MyPair selectedPair,
                                       int width = 150,
                                       bool SubmitForm = true,
                                       bool AddBlankRow = false,
                                       bool AutoPostback = true,
                                       string Tooltip = "",
                                       bool Enabled = true,
                                       string ddMsg = "",
                                       string SelectedValue = "")
        {
            jqDropList dd = new jqDropList(Name, PageName, SubmitForm)
            {
                selectedItemIndex = -1,
                id = id_prefix + Name,
                autoPostBack = AutoPostback,
                toolTip = Tooltip,
                style = "width: " + width + "px;",
                enabled = Enabled
            };

            selectedPair = new MyPair();

            //Add a blank area to the top of the list
            if (AddBlankRow)
                dd.AddItem(ddMsg, "", false);

            if (Options != null)
            {
                for (int i = 0; i < Options.Count; i++)
                {
                    bool sel = (i == selected) || (Options[i].ValueStr == SelectedValue);
                    if (sel)
                        selectedPair = Options[i];
                    dd.AddItem(Options[i].Name, Options[i].ValueStr, sel);
                }
            }

            if (dd.selectedItemIndex == -1 && AddBlankRow)
                dd.selectedItemIndex = 0;

            return dd.Build();
        }
        #endregion DropDown

        #region TimeSpanPicker

        protected string FormTimeSpanPicker( string Name,
                                             string Label = "",
                                             bool showSeconds = true,
                                             bool showDays = false,
                                             TimeSpan defaultTimeSpan = default(TimeSpan),
                                             bool SubmitForm = true)
        {
            UsesjqTimeSpanPicker = true;

            jqTimeSpanPicker ts = new jqTimeSpanPicker(Name, Label, this.PageName, SubmitForm)
            {
                toolTip = "This is the tooltip for the timespan picker",
                showSeconds = showSeconds,
                showDays = showDays,
                defaultTimeSpan = defaultTimeSpan
            };
            return ts.Build();
        }

        #endregion TimeSpanPicker

        #region Helpers

        /// <summary>
        /// Generate jqRadioButton for given Enum type
        /// </summary>
        /// <param name="en">The Enum value</param>
        /// <param name="name">Control ID</param>
        /// <returns></returns>
        public string RadioButtonEnum(Enum en, string name, bool enabled = true, string[] names = null)
        {
            name = $"_radio_{name}";

            jqRadioButton rb = new jqRadioButton(name, this.PageName, true)
            {
                buttonset = false,
                enabled = enabled
            };

            foreach (int i in Enum.GetValues(en.GetType()))
            {
                string s = Enum.GetName(en.GetType(), i);

                if (s == en.ToString())
                    rb.@checked = $"{i}";

                if (names != null)
                    s = names[i];

                if(s!="-" && s!="unknown")
                    rb.values.Add($"{s}", $"{i}");
            }

            return rb.Build();
        }

        /// <summary>
        /// Process radio button control selection
        /// For radio buttons the selected value is appended to control id (after underscore)
        /// i.e.
        /// id = _radio_grp_2_logic_1
        /// _radio_grp_2_logic = 1
        /// </summary>
        /// <param name="ctrlID"></param>
        /// <param name="value"></param>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static bool ProcessRadioBtn(ref string ctrlID, ref string value, NameValueCollection parts)
        {
            // i.e. "_radio_ctrlType_1" - where last _1 means selected value
            GroupCollection match = Regex.Match(ctrlID, @"radio_(.+)_(\d)").Groups;
            if (match.Count == 3)
                try
                {
                    ctrlID = match[1].Value;
                    value = parts[$"_radio_{ctrlID}"];
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Can't process {ctrlID}: {ex.Message}");
                }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="table"></param>
        public void TableObjectProperties(object o, TableBuilder table)
        {
            if (o == null)
                return;

            string[] hdr = { $"'{o}'", "Value" };
            table.AddRowHeader(hdr);

            FieldInfo[] fields = o.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                TableBuilder.TableRow row = table.AddRow();
                row.AddCell(field.Name);
                object val = field.GetValue(o);
                if (val is Array)
                {
                    string s = "";
                    foreach (var v in (val as Array))
                    {
                        s += (v != null ? v.ToString() : "") + ",";
                    }
                    val = s;
                }
                string sval = val != null ? val.ToString() : "";
                if (sval.Length > 125)
                    sval = sval.Substring(0, 125) + " ...";
                row.AddCell(sval);
            }
        }


        /// <summary>
        /// Create string "name='value'" if val>=0
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="def">Default string if val==-1</param>
        /// <returns></returns>
        public static string fmtHTMLPair(string name, int value, string def = "")
        {
            return fmtHTMLPair(name, (value >= 0 ? value.ToString() : null), def);
        }
        public static string fmtHTMLPair(string name, string value, string def = "")
        {
            return (!String.IsNullOrEmpty(value) ? $"{name}='{value}'" : def);
        }

        /// <summary>
        /// Create HTML for image
        /// </summary>
        /// <param name="file"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static string HTML_Img(string file, int width=-1, int height=-1)
        {
            return $"<img src='{file}' {fmtHTMLPair("width", width)} {fmtHTMLPair("height", height)}/>";
        }

        /// <summary>
        /// Return "check" image HTML if val==true
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string BoolToImg(bool val)
        {
            return val ? HTML_Img("images/HomeSeer/ui/active.png", 15) : "";
        }


        /// <summary>
        /// Create a Menu button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="label"></param>
        /// <param name="url">Can be a link, not button</param>
        /// <returns></returns>
        public string MenuButton(string id, string label, string CurrentTab = null, string url = null)
        {
            bool selected = (CurrentTab!=null) ? (id == CurrentTab) : false;
            string style = "font-size: 100%; padding: 3px 0; width: 120px !important;" + (selected ? " border: 1px solid #5799DF;" : "");
            return FormButton( Name: id,
                               label: label,
                               url: url,
                               Style: style,
                               className: selected ? "functionrowbuttonselected" : "functionrowbutton",
                               width: 120
                               );
        }

        /// <summary>
        /// Generate HTML for Delete button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string DeleteBtn(string id, string ToolTip = "", string value = "", bool small = false)
        {
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/Delete.png", width: small ? 15 : 0);
        }

        /// <summary>
        /// Generate HTML for Add button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string AddBtn(string id, string ToolTip = "", string value = "", bool small = false)
        {
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/Add.png", width: small ? 15 : 0);
        }

        /// <summary>
        /// Generate HTML for Add button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string CopyBtn(string id, string ToolTip = "", string value = "", bool small = false)
        {
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/duplicate.png", width: small ? 15 : 0);
        }

        /// <summary>
        /// Generate HTML for Edit button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string EditBtn(string id, string ToolTip = "", string value = "", bool small = false)
        {
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/Edit.gif", width: small ? 15 : 0);
        }

        /// <summary>
        /// Generate HTML for Save button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string SaveBtn(string id, string ToolTip = "", string value = "", bool small = false)
        {
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/Save.png", width: small ? 15 : 0);
        }

        /// <summary>
        /// Generate HTML for Edit button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public string NoteBtn(string id, string ToolTip = "", string value = "checked", bool small = false)
        {
            // label: "checked" - immitate checkbox click
            return FormButton(id, label: value, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/note.gif", width: small ? 15 : 0);
        }

        public static string RunBtnPrefix = "set_";
        // To generate unique btn id on the page (ignored, but necessary)
        private static int runBtnId = 0;

        /// <summary>
        /// Generate HTML for Run button
        /// </summary>
        /// <param name="devID"></param>
        /// <param name="value"></param>
        /// <param name="ToolTip"></param>
        /// <param name="small"></param>
        /// <returns></returns>
        public string RunBtn(int devID, object value, string Label = "", bool small = false)
        {
            string ToolTip = $"Test '{Label} ({value})' Ref. {devID}";
            string id = $"{RunBtnPrefix}{++runBtnId}_{devID}";
            string valstr = $"{value}";
            return FormButton(id, valstr, ToolTip: ToolTip, ImagePathNormal: "images/HomeSeer/ui/run-event.png", width: small ? 15 : 0);
        }

        /// <summary>
        /// Extract devID from string created by RunBtn()
        /// </summary>
        /// <param name="devIdStr"></param>
        /// <returns></returns>
        public static int ExtractCtrlIdRunBtn(string devIdStr)
        {
            devIdStr = devIdStr.Replace(RunBtnPrefix, "");
            string[] s = devIdStr.Split('_');
            int.TryParse(s[1], out int devID);
            return devID;
        }


        /// <summary>
        /// Generate HTML for Edit button
        /// </summary>
        /// <param name="id"></param>
        /// <param name="title">Tooltip</param>
        /// <returns></returns>
        public static string MyToolTip(string text, bool addspace=true, string url = null, bool error = false, bool small = true, string image = null)
        {
            jqButton b = new jqButton($"tt{++toolTipCnt}", text, null, false)
            {
                url = url,
                enabled = (url!=null),
                hyperlink = false,
                urlNewWindow = true,
                imagePathNormal = image!=null ? image : error ? "images/HomeSeer/ui/Caution-red.png" : "images/HomeSeer/ui/help-button-sm.png",
                toolTip = text,
                width = small ? (error? 15 : 10) :  20
            };

            string space = addspace ? "&nbsp;&nbsp;" : "";
            return space + b.Build().Replace("</button>\r\n", "</button>").Trim();
        }

        private static int toolTipCnt = 0;



        /// <summary>
        /// Convert a list string to a table, for inserting inside another table cell
        /// </summary>
        /// <param name="lst">List of values in a string "a,b,c"</param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string ListToTable(string lst, char sep = ',')
        {
            string[] strings = lst.Split(sep);
            string attrs = "max_width='300px' style='margin: 5px; display: inline-table'";
            var table = new TableBuilder(null, ncols: strings.Length, attrs: attrs);

            foreach (string s in strings)
            {
                table.AddCell(s.Trim());
            }

            return table.Build(addBR: false);
        }

        #endregion Helpers
    }
}