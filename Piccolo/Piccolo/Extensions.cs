﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMD.HCIL.Piccolo
{
    public static class Extensions
    {

        /// <summary>
        /// Creates the smallest possible rectangle that contains all the provided rectangles
        /// </summary>
        /// <param name="rectangles"></param>
        /// <returns></returns>
        public static RectangleF BoundingRect(this IEnumerable<RectangleF> rectangles)
        {
            RectangleF result = RectangleF.Empty;
            foreach (var rect in rectangles)
            {
                if (result != RectangleF.Empty)
                {
                    if (rect != RectangleF.Empty)
                    {
                        result = RectangleF.Union(result, rect);
                    }
                }
                else
                {
                    result = rect;
                }
            }
            return result;
        }
    }
}