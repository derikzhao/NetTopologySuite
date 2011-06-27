using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Noding;

namespace NetTopologySuite.Geometries.Prepared
{
    ///<summary>
    /// Computes the <tt>intersects</tt> spatial relationship predicate
    /// for a target <see cref="PreparedLineString"/> relative to other <see cref="IGeometry"/> classes.
    ///</summary>
    /// <remarks>
    /// Uses short-circuit tests and indexing to improve performance. 
    /// </remarks>
    /// <author>Martin Davis</author>
    public class PreparedLineStringIntersects
    {
        ///<summary>
        /// Computes the intersects predicate between a <see cref="PreparedLineString"/>
        /// and a <see cref="IGeometry"/>.
        ///</summary>
        /// <param name="prep">The prepared linestring</param>
        /// <param name="geom">A test geometry</param>
        /// <returns>true if the linestring intersects the geometry</returns>
        public static bool Intersects(PreparedLineString prep, IGeometry geom)
        {
            PreparedLineStringIntersects op = new PreparedLineStringIntersects(prep);
            return op.Intersects(geom);
        }

        protected PreparedLineString prepLine;

        ///<summary>
        /// Creates an instance of this operation.
        /// </summary>
        /// <param name="prepLine">The target PreparedLineString</param>
        public PreparedLineStringIntersects(PreparedLineString prepLine)
        {
            this.prepLine = prepLine;
        }

        ///<summary>
        /// Tests whether this geometry intersects a given geometry.
        ///</summary>
        /// <param name="geom">The test geometry</param>
        /// <returns>true if the test geometry intersects</returns>
        public bool Intersects(IGeometry geom)
        {
            /*
             * If any segments intersect, obviously intersects = true
             */
            IList<ISegmentString> lineSegStr = SegmentStringUtil.ExtractSegmentStrings(geom);
            bool segsIntersect = prepLine.IntersectionFinder.Intersects(lineSegStr);
            // MD - performance testing
            //		boolean segsIntersect = false;
            if (segsIntersect)
                return true;

            /*
             * For L/L case we are done
             */
            if (geom.Dimension == Dimension.Curve) return false;

            /*
             * For L/A case, need to check for proper inclusion of the target in the test
             */
            if (geom.Dimension == Dimension.Surface
                    && prepLine.IsAnyTargetComponentInTest(geom)) return true;

            /*
             * For L/P case, need to check if any points lie on line(s)
             */
            if (geom.Dimension == Dimension.Point)
                return IsAnyTestPointInTarget(geom);

            //		return prepLine.getGeometry().intersects(geom);
            return false;
        }

        ///<summary>
        /// Tests whether any representative point of the test Geometry intersects
        /// the target geometry.
        ///</summary>
        /// <remarks>
        /// Only handles test geometries which are Puntal (dimension 0)
        /// </remarks>
        /// <param name="testGeom">A Puntal geometry to test</param>
        /// <returns>true if any point of the argument intersects the prepared geometry</returns>
        protected bool IsAnyTestPointInTarget(IGeometry testGeom)
        {
            /*
             * This could be optimized by using the segment index on the lineal target.
             * However, it seems like the L/P case would be pretty rare in practice.
             */
            PointLocator locator = new PointLocator();
            IList<ICoordinate> coords = ComponentCoordinateExtracter.GetCoordinates(testGeom);
            foreach (ICoordinate p in coords)
            {
                if (locator.Intersects(p, prepLine.Geometry))
                    return true;
            }
            return false;
        }

    }
}
