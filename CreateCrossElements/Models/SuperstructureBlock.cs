using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateCrossElements.Models
{
    public class SuperstructureBlock
    {
        public Element BlockElement { get; set; }
        public Line BlockAxis { get; set; }
        public int CountCrossSection { get; set; }
        public List<double> BlockParameters { get; set; }

        public SuperstructureBlock(Document doc, Element blockElem)
        {
            BlockElement = blockElem;
            BlockAxis = GetBlockAxis(doc, blockElem);
            CountCrossSection = GetCountCrossSection();
            BlockParameters = GetCrossSectionPlacementParameters();
        }

        public List<XYZ> GetPointsOnAxis()
        {
            var points = new List<XYZ>(CountCrossSection);
            foreach(var parameter in BlockParameters)
            {
                points.Add(BlockAxis.Evaluate(parameter, true));
            }

            return points;
        }

        // Получение линии по низу блока
        private static Line GetBlockAxis(Document doc, Element blockElem)
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
