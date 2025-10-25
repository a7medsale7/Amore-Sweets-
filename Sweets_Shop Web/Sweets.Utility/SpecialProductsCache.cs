using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweets.Utility
{
    public class SpecialProductsCache
    {
        private static List<int> _specialProductIds = new List<int>();

        public static List<int> GetAll() => _specialProductIds;

        public static void Add(int productId)
        {
            if (!_specialProductIds.Contains(productId))
                _specialProductIds.Add(productId);
        }

        public static void Remove(int productId)
        {
            _specialProductIds.Remove(productId);
        }

        public static bool IsSpecial(int productId)
        {
            return _specialProductIds.Contains(productId);
        }
    }
}
