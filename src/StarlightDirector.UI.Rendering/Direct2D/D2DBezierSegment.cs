﻿using System.Drawing;
using SharpDX.Direct2D1;
using StarlightDirector.UI.Rendering.Extensions;

namespace StarlightDirector.UI.Rendering.Direct2D {
    public struct D2DBezierSegment {

        public PointF Point1 { get; set; }

        public PointF Point2 { get; set; }

        public PointF Point3 { get; set; }

        internal BezierSegment ToNative() {
            return new BezierSegment {
                Point1 = Point1.ToD2DVector(),
                Point2 = Point2.ToD2DVector(),
                Point3 = Point3.ToD2DVector()
            };
        }

    }
}
