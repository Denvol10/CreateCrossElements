using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CreateCrossElements.Models
{
    public class SuperstructureBlock
    {
        public Element BlockElement { get; set; }
        public double BlockHeight { get; set; }
        public Line BlockAxis { get; set; }
        public int CountCrossSection { get; set; }
        public List<double> BlockParameters { get; set; }
        public List<XYZ> PointsOnAxis { get; set; }
        public XYZ NormalVector { get; set; }
        public XYZ YVector { get; set; }
        private double _distanceBetweenAdaptivePoints = UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Meters);

        public SuperstructureBlock(Document doc, Element blockElem, double height)
        {
            BlockElement = blockElem;
            BlockHeight = UnitUtils.ConvertToInternalUnits(height, UnitTypeId.Millimeters);
            BlockAxis = GetBlockAxis(doc, blockElem);
            CountCrossSection = GetCountCrossSection();
            BlockParameters = GetCrossSectionPlacementParameters();
            PointsOnAxis = GetPointsOnAxis();
            NormalVector = GetNormalVector(doc);
            YVector = GetYVector(doc);
        }

        //public List<XYZ> GetFirstCrossElementPoints()
        //{
        //    var points = PointsOnAxis.Select(p => p + NormalVector * BlockHeight).ToList();

        //    return points;
        //}

        //public List<XYZ> GetSecondCrossElementPoints()
        //{
        //    var points = PointsOnAxis.Select(p => p + NormalVector * (BlockHeight - _distanceBetweenAdaptivePoints)).ToList();

        //    return points;
        //}

        //public List<XYZ> GetThirdCrossElementPoints(bool isChangeSite)
        //{
        //    var points = PointsOnAxis.Select(p => p + NormalVector * BlockHeight + YVector * _distanceBetweenAdaptivePoints).ToList();

        //    return points;
        //}

        public List<(XYZ First, XYZ Second, XYZ Third)> GetPointsForCrossElements(bool isChangeSite, double rotationAngle)
        {
            var adaptivePoints = new List<(XYZ, XYZ, XYZ)>(CountCrossSection);
            rotationAngle = UnitUtils.ConvertToInternalUnits(rotationAngle, UnitTypeId.Degrees);
            foreach(var axisPoint in PointsOnAxis)
            {
                XYZ firstPoint = axisPoint + NormalVector * BlockHeight;
                XYZ secondPoint = axisPoint + NormalVector * (BlockHeight - _distanceBetweenAdaptivePoints);
                XYZ thirdPoint;
                if (isChangeSite)
                {
                    thirdPoint = firstPoint + YVector * _distanceBetweenAdaptivePoints;
                }
                else
                {
                    thirdPoint = firstPoint + YVector.Negate() * _distanceBetweenAdaptivePoints;

                }

                if(rotationAngle != 0)
                {
                    var transform = Transform.CreateRotationAtPoint(firstPoint - secondPoint, rotationAngle, firstPoint);
                    thirdPoint = transform.OfPoint(thirdPoint);
                }

                adaptivePoints.Add((firstPoint, secondPoint, thirdPoint));
            }

            return adaptivePoints;
        }

        private List<XYZ> GetPointsOnAxis()
        {
            var points = new List<XYZ>(CountCrossSection);
            foreach (var parameter in BlockParameters)
            {
                points.Add(BlockAxis.Evaluate(parameter, true));
            }

            return points;
        }

        private XYZ GetNormalVector(Document doc)
        {
            var blockAdaptivePoints = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(BlockElement as FamilyInstance);
            var firstReferencePoint = doc.GetElement(blockAdaptivePoints.FirstOrDefault()) as ReferencePoint;
            var secondReferencePoint = doc.GetElement(blockAdaptivePoints.ElementAt(1)) as ReferencePoint;
            var thirdReferencePoint = doc.GetElement(blockAdaptivePoints.ElementAt(2)) as ReferencePoint;

            XYZ firstPoint = firstReferencePoint.Position;
            XYZ secondPoint = secondReferencePoint.Position;
            XYZ thirdPoint = thirdReferencePoint.Position;

            XYZ normalVector = (secondPoint - firstPoint).CrossProduct(thirdPoint - firstPoint).Normalize();

            if (normalVector.Z < 0)
            {
                normalVector = normalVector.Negate();
            }

            return normalVector;
        }

        private XYZ GetYVector(Document doc)
        {
            var blockAdaptivePoints = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(BlockElement as FamilyInstance);
            var firstReferencePoint = doc.GetElement(blockAdaptivePoints.FirstOrDefault()) as ReferencePoint;
            var thirdReferencePoint = doc.GetElement(blockAdaptivePoints.ElementAt(2)) as ReferencePoint;

            XYZ firstPoint = firstReferencePoint.Position;
            XYZ thirdPoint = thirdReferencePoint.Position;

            XYZ yVector = (thirdPoint - firstPoint).Normalize();

            return yVector;
        }

        // Получение линии по низу блока
        private Line GetBlockAxis(Document doc, Element blockElem)
        {
            var blockAdaptivePoints = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(blockElem as FamilyInstance);
            var firstReferencePoint = doc.GetElement(blockAdaptivePoints.FirstOrDefault()) as ReferencePoint;
            var secondReferencePoint = doc.GetElement(blockAdaptivePoints.ElementAt(1)) as ReferencePoint;
            XYZ firstPoint = firstReferencePoint.Position;
            XYZ secondPoint = secondReferencePoint.Position;

            XYZ alongVector = (secondPoint - firstPoint).Normalize();
            Parameter lengthParameter = blockElem.LookupParameter("Длина блока");
            double length = lengthParameter.AsDouble();
            XYZ secondBlockCurvePoint = firstPoint + alongVector * length;

            Line blockAxis = Line.CreateBound(firstPoint, secondBlockCurvePoint);

            return blockAxis;
        }

        private int GetCountCrossSection()
        {
            var parameters = BlockElement.Parameters;
            int countVisibleCrossSection = 0;
            foreach (Parameter parameter in parameters)
            {
                string parameterName = parameter.Definition.Name;
                if (parameterName.Contains("Ребро") && parameterName.Contains("Видимость"))
                {
                    if (parameter.AsInteger() == 1)
                    {
                        countVisibleCrossSection += 1;
                    }
                }
            }

            return countVisibleCrossSection;
        }

        private List<double> GetCrossSectionPlacementParameters()
        {
            //var placementParameters = new List<double>(CountCrossSection);
            var positionParameters = new List<(Parameter Parameter, int Number)>();

            var parameters = BlockElement.Parameters;
            foreach (Parameter parameter in parameters)
            {
                string parameterName = parameter.Definition.Name;
                if (parameterName.Contains("Ребро") && parameterName.Contains("Положение"))
                {
                    var number = int.Parse(parameterName.Split('_').ElementAt(1));
                    positionParameters.Add((parameter, number));
                }
            }

            var placementParameters = positionParameters.OrderBy(p => p.Number)
                                                        .Take(CountCrossSection)
                                                        .Select(p => p.Parameter.AsDouble())
                                                        .ToList();

            return placementParameters;
        }
    }
}
