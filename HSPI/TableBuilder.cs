using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using HomeSeerAPI;
using Scheduler;
using static Scheduler.clsJQuery;

namespace HSPI_AKTemplate
{
    public class TableBuilder
    {
        /// <summary>
        /// Bu default display tables in sliding tab
        /// </summary>
        public static bool InSliderDefault = true;

        private StringBuilder stb = new StringBuilder();
        private readonly int ncols = 0;
        private bool row_started = false;
        private object header = null;
        private bool inSlider = false;
        public string tableID { get; private set; }    // ID for table and SliderTab
        private int ncols_row = 0;      // To make sure each row has 'ncols' cells
        public string page_name = null; // If want to add sorting to table columns, see AddHeader
        public bool sorthdr = false;    // If want to add sorting to table columns, see AddHeader
        public string sortby = null;    // If want to add sorting to table columns, see AddHeader
        public string sortorder = null; // If want to add sorting to table columns, see AddHeader

        /// <summary>
        /// To generate table IDs if none is passed
        /// </summary>
        private static int tableCnt = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="header"><Can be a string, or string array/param>
        /// <param name="ncols">If header is a string then also need ncols</param>
        /// <param name="tooltip"></param>
        /// <param name="klass"></param>
        /// <param name="border"></param>
        /// <param name="width"></param>
        /// <param name="attrs"></param>
        /// <param name="page_name">If want to add sorting to table columns, see AddHeader/BuildHeaderLink</param>
        /// <param name="sorthdr">As above</param>
        /// <param name="sortby">As above</param>
        /// <param name="sortorder">As above</param>
        /// <param name="tableID">ID for table and SliderTab</param>
        /// <param name="inSlider">Put table inside SliderTab</param>
        public TableBuilder( object header,
                             int ncols = 0,
                             string tooltip = null,
                             string klass = "",
                             int border = -1,
                             string width = null,
                             string attrs = "",
                             string page_name = null,
                             bool sorthdr = false,
                             string sortby = null,
                             string sortorder = "asc",
                             string tableID = null,
                             bool ?inSlider = null)
       {
            if (tableID == null)
                tableID = $"table{++tableCnt}";
            this.tableID = tableID;

            // If not specified - get from settings
            if (inSlider == null)
                inSlider = InSliderDefault;

            this.inSlider = (bool)inSlider;

            // Get ncols from header if it's an array
            if (header != null)
            {
                Type hdrType = header.GetType();
                if (hdrType.IsArray && ncols <= 0)
                    ncols = (header as Array).Length;
            }

            this.ncols = ncols;
            this.page_name = page_name;
            this.sorthdr = sorthdr;
            this.sortby = sortby;
            this.sortorder = sortorder;

            if (tooltip != null)
            {
                header += PageBuilder.MyToolTip(tooltip);
            }

            attrs += " cellpadding = '0' cellspacing = '0' ";
            // Add border if specified
            attrs += PageBuilder.fmtHTMLPair("border", border, "");
            // Add width if specified
            attrs += PageBuilder.fmtHTMLPair("width", width, "");
            // Add width if specified
            attrs += PageBuilder.fmtHTMLPair("id", tableID, "");

            if (width == null)
                klass += " full_width_table_99_percent"; // full_width_table, full_width_table_100_percent

            string html = $"<table {attrs} class='{klass}'>";
            stb.Append(html);

            AddTableHeader(header);
        }

        /// <summary>
        /// Table Header
        /// </summary>
        /// <param name="header">Can be string or array of strings</param>
        /// <param name="klass"></param>
        /// <param name="attrs"></param>
        public void AddTableHeader(object header, string klass = "tableheader", string attrs = "")
        {
            if (!inSlider)
            {
                AddHeader(header, klass: klass, attrs: attrs);
                if(header!=null)
                    AddBlankRow();
            }
            else
            {
                this.header = header;
            }
        }

        /// <summary>
        /// Row Header
        /// </summary>
        /// <param name="header">Can be string or array of strings</param>
        /// <param name="klass"></param>
        /// <param name="attrs"></param>
        public void AddRowHeader(object header, string klass = "tablecolumn", string attrs = "")
        {
            AddHeader(header, klass: klass, attrs: attrs);
        }

        /// <summary>
        /// Generic header, i.e. table or column header
        /// If header is array - convert header strings to link for sorting the table
        /// </summary>
        /// <param name="header">Can be string or array of strings</param>
        /// <param name="klass"></param>
        /// <param name="attrs"></param>
        /// <param name="hdr_btn"></param>
        public void AddHeader(object header, string klass = "", string attrs = "")
        {
            if (header == null)
                return;

            Type hdrType = header.GetType();

            // Make background image ui-bg_highlight-soft_d1x16.png a bit prettier
            // TEMP - TODO: Use custom css
            attrs += " style='padding:5px 5px; background-position-y: -20px; background-size: 100% 180%; background-origin: border-box;' ";

            AddRow(klass: klass);

            if (hdrType.IsArray)
            {
                foreach (string s in (header as Array))
                {
                    string html = BuildHeaderLink(s);

                    AddColHeader(html, colspan: 1, klass: klass, attrs: attrs);
                }
            }
            else
            {
                AddColHeader(header as string, colspan: this.ncols, klass: klass, attrs: attrs);
            }

            EndRow();
        }

        public void AddColHeader(string html, int colspan = 1, string klass = "tablecolumn", string attrs = "")
        {
            AddCell(html, colspan: colspan, klass: klass, attrs: attrs);
        }

        /// <summary>
        /// Add row to the table
        /// </summary>
        /// <param name="html">If not null - insert the html in the row with colspan=table.ncols</param>
        /// <param name="attrs"></param>
        /// <param name="klass">'class' for the row</param>
        /// <param name="td_klass">'class' for the cell in case 'html' isn't null</param>
        /// <returns>TableRow object exposing AddCell() function</returns>
        public TableRow AddRow(string html = null, string attrs = "", string klass = "tablecell", string td_klass = "tablecell")
        {
            EndRow();
            stb.Append( $"  <tr {attrs} class='{klass}'>" );
            row_started = true;

            if (html != null)
            {
                AddCell(html, colspan: this.ncols, klass: td_klass);
                EndRow();
            }

            return new TableRow(this);
        }

        public void AddBlankRow(string attrs = "", string klass = "tablerowUplugineven")
        {
            AddRow("", attrs, klass: klass, td_klass: "tablecellblank");
        }

        public void EndRow()
        {
            if (row_started)
            {
                // Add <td> until ncols_row == ncols, 
                // note: ncols_row is incremented in AddCell()
                while (ncols_row < ncols)
                {
                    AddCell("---");
                }

                stb.Append("  </tr> ");
            }
            row_started = false;
            ncols_row = 0;
        }

        public void AddCell(string html, int colspan = 1, string klass = "device_status_image"/*"tablecell"*/, string attrs = "", string type = "td", string width = null)
        {
            if (html == null) html = "";

            // If cell is just an image (i.e. check mark) - align it in center
            bool isImg = Regex.Match(html, @"^<img (.+)/>").Groups.Count>1;
            if (isImg)
                attrs += " style='text-align: center;'";

            attrs += PageBuilder.fmtHTMLPair("width", width);

            html = $"<{type} {attrs} class='{klass}' colspan='{colspan}'>{html}</{type}>";
            stb.Append(html);
            ncols_row += colspan;
        }

        /// <summary>
        /// If page_name is passed - convert headers to buttons for sorting columns
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string BuildHeaderLink(string text)
        {
            if (this.page_name == null || !sorthdr)
                return text;

            // Toggle dec/asc
            // The link query string 'sortorder' should be opposite to current sortorder
            string sorder_inv = (this.sortorder == "asc") ? "dec" : "asc";
            string arrow = (this.sortorder == "asc") ? "&uarr;" : "&darr;";

            string query = $"?sortby={text}&sortorder={sorder_inv}";
            string label = text;
            if (label == this.sortby)
            {
                label += " (" + arrow + ")";
            }

            return  $"<a target='_self' class='device_management_name' title='{text}' href='{this.page_name}{query}'>{label}</a>";
        }

        /// <summary>
        /// Complete table HTML and (optionally) put table HTML inside SliderTab
        /// </summary>
        /// <param name="addBR">Add <BR> after the table</param>
        /// <param name="page_name">Page Name for SliderTab postback</param>
        /// <param name="initiallyOpen">for SliderTab</param>
        /// <returns></returns>
        public string Build(bool addBR = true, bool initiallyOpen = true, string titleAlt = null)
        {
            EndRow();
            stb.Append(" </table>");

            string html = stb.ToString();
            // Quick workaround, if it's table inside table - make it 100%, not 99%
            if (!addBR)
                html = html.Replace("full_width_table_99_percent", "full_width_table_100_percent");

            if(inSlider && this.header!=null)
            {
                // Put table HTML inside SliderTab
                // Note: if change format of "slide_{tableID}" or "tab_{tableID}" - update TrySliderSet() too!
                var st = new jqSlidingTab(SliderName(tableID), this.page_name, true) //$"slide_{tableID}"
                {
                    initiallyOpen = initiallyOpen,
                    callGetOnOpenClose = false,
                    submitForm = true
                };
                st.tab.AddContent(html);
                st.tab.name = TabName(tableID);
                st.tab.tabName.Selected = this.header.ToString();
                if(titleAlt != null)
                    st.tab.tabName.Unselected = titleAlt;
                html = st.Build();
            }

            if (addBR)
                html += "<br>";

            return html;
        }

        /// <summary>
        /// Build table from given 'data'
        /// </summary>
        /// <param name="data">TableData</param>
        /// <param name="hdr">Array of header strings</param>
        /// <param name="title">Table title (displayed in large font)</param>
        /// <param name="tooltip">Add tooltip to title</param>
        /// <param name="page_name">If want to add sorting to table columns, see AddHeader, also for SliderTab</param>
        /// <param name="sortby">as above</param>
        /// <param name="sortorder">as above</param>
        /// <param name="noempty">Don't show empty table</param>
        /// <param name="addBR">Add <BR> after table</param>
        /// <param name="inSlider"></param>
        /// <param name="tableID"></param>
        /// <param name="initiallyOpen"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        public static string BuildTable( TableData data,
                                         string[] hdr = null,
                                         string title = null,
                                         string tooltip = null,
                                         string page_name = null,
                                         bool sorthdr = false,
                                         string sortby = null,
                                         string sortorder = "asc",
                                         bool noempty = false,
                                         bool addBR = true,
                                         bool? inSlider = null,
                                         string tableID = null,
                                         bool initiallyOpen = true,
                                         string width = null,
                                         string[] widths = null
                                       )
        {
            if (inSlider == null)
                inSlider = InSliderDefault;

            // If no title, but header array - don't use slider
            if (title == null && hdr != null)
                inSlider = false;

            // If 'noempty' is true - don't display empty table at all
            if ((data==null) || (data.Count == 0 && noempty))
                return "";

            // Work out ncols, either from hdr.Length or lenght of first row
            int ncols = (hdr!=null) ? hdr.Length : data[0].Count;

            // 
            object _hdr_ = (title=="notitle") ? null : (title!=null) ? title : (object)hdr;

            TableBuilder table = new TableBuilder( _hdr_, ncols, tooltip,
                                                   page_name: page_name,
                                                   sorthdr: sorthdr,
                                                   sortby: sortby,
                                                   sortorder: sortorder,
                                                   inSlider: inSlider,
                                                   tableID: tableID,
                                                   width: width
                                                  );

            // If pass 'title' - then 'hdr' is displayed in second row
            if (hdr != null && title != null)
            {
                //if(title != "notitle" )
                //    table.AddBlankRow();
                table.AddRowHeader(hdr);
            }

            // Work out sort column
            // Note: 'sortby' should be from this array
            if (sortby != null)
            {
                int sort_col = Array.IndexOf(hdr, sortby);
                Debug.Assert(sort_col >= 0);
                if (sort_col >= 0)
                {
                    IComparer<List<string>> comparer = new ComparingClass(sort_col, sortorder);
                    data.Sort(comparer);
                }
            }

            // Add rows from 'data'
            foreach (var row in data)
            {
                table.AddRow();
                Debug.Assert(row.Count <= ncols);

                for (int i = 0; i<row.Count; i++)
                {
                    table.AddCell(row[i], colspan: row.Count == 1 ? ncols : 1, width: (widths!=null && i<widths.Length) ? widths[i] : null);
                }

                table.EndRow();
            }

            return table.Build(addBR, initiallyOpen);
        }


        public static string SliderName(string name)
        {
            Debug.Assert(!name.Contains("slide_"));
            return "slide_" + name;
        }
        public static string TabName(string name)
        {
            Debug.Assert(!name.Contains("tab_"));
            return "tab_" + name;
        }

        #region Helper Classes

        public class TableRow
        {
            private TableBuilder tb;
            public TableRow(TableBuilder tb)
            {
                this.tb = tb;
            }

            public void AddCell(string html, int colspan = 1, string klass = "device_status_image"/*"tablecell"*/, string attrs = "", string type = "td")
            {
                tb.AddCell(html, colspan, klass, attrs, type);
            }
        }


        /// <summary>
        /// IComparer implementation used for sorting List<List<string>> by 'sort_col'
        /// Also handles url links by extaracting text only
        /// Also handles proper int compare
        /// </summary>
        public class ComparingClass : IComparer<List<string>>
        {
            string sortorder = "asc";
            int sort_col = 0;

            public ComparingClass(int sort_col = 0, string sortorder = "asc")
            {
                this.sortorder = sortorder;
                this.sort_col = sort_col;
            }

            private string get(List<string> lst)
            {
                string s = lst[sort_col];

                // Check is string is a link <a> - then extract only text
                GroupCollection grps = Regex.Match(s, @"<a (.+)>(.+)</a>").Groups;
                if(grps.Count>2)
                {
                    s = grps[2].Value;
                }

                // Check for 'int'
                int n;
                if( int.TryParse(s, out n) )
                {
                    s = String.Format("{0:00000000}", n);
                }

                return s;
            }

            int IComparer<List<string>>.Compare(List<string> x, List<string> y)
            {
                string xs = get(x);
                string ys = get(y);

                return (sortorder=="asc") ? xs.CompareTo(ys) :  ys.CompareTo(xs);
            }
        }



        public class TableDataRow : List<string>
        {
        }

        /// <summary>
        /// Class TableData, List<List<string>>
        /// </summary>
        public class TableData : List<TableDataRow>
        {

            public TableDataRow AddRow()
            {
                int n = Count;
                Add(new TableDataRow());
                return this[n];
            }
        }

        #endregion Helper Classes

    }
}
