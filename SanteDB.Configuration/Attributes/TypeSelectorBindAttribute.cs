using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configuration.Attributes
{
    /// <summary>
    /// Type selection bind attribute
    /// </summary>
    public class TypeSelectorBindAttribute : Attribute
    {

        /// <summary>
        /// Creates a new type selector bind
        /// </summary>
        public TypeSelectorBindAttribute(Type bindType)
        {
            this.BindType = bindType;
        }

        /// <summary>
        /// Gets or sets the binding type for the dropdown
        /// </summary>
        public Type BindType { get; set; }

    }
}
