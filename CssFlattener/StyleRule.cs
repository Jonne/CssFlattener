using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CssFlattener
{
    public class StyleRule
    {
        public string Selector
        {
            get;
            set;
        }

        public Dictionary<string, string> Declarations
        {
            get;
            set;
        }

        public int Index
        {
            get;
            set;
        }
    }
}
