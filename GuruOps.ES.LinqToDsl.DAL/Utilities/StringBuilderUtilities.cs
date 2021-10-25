using System.Text;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class StringBuilderUtilities
    {
        //Hacked some string to be similar; last resolve :( Hoopefully find much better idea.
        public static StringBuilder Hacksomechar(this StringBuilder val)
        {
            return val.Replace(";", string.Empty);
        }
    }
}
