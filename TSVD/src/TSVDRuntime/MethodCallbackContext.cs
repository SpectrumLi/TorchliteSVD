﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSVDRuntime
{
    public class MethodCallbackContext
    {
        /// <summary>
        /// Gets or sets object instance.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// Gets or sets field id.
        /// </summary>
        public string FieldId { get; set; }

        /// <summary>
        /// Gets or sets caller method.
        /// </summary>
        public string Caller { get; set; }

        /// <summary>
        /// Gets or sets iLOffset where the method is called.
        /// </summary>
        public int ILOffset { get; set; }

        /// <summary>
        /// Gets or sets called method.
        /// </summary>
        public string Callee { get; set; }
    }


}
