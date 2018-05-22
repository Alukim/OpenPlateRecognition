using System.Collections.Generic;

namespace LicensePlateRecognition
{
    public static class DetectChars
    {
        public static List<PossiblePlate> DetectCharsInPlates(List<PossiblePlate> listOfPossiblePlates)
        {
            var recognizedPlates = new List<PossiblePlate>();
            for (int i = 0; i < listOfPossiblePlates.Count; i++)
            {
                var fileName = $"{i.ToString()}.png";
                var possiblePlate = listOfPossiblePlates[i];
                possiblePlate.ImgPlate.SaveImage(fileName);
                var cognitiveServiceHttpClientProvider = new CognitiveServiceHttpClientProvider();
                var recognizedString = cognitiveServiceHttpClientProvider.MakeAnalysisRequest(fileName).Result;
                if (!string.IsNullOrEmpty(recognizedString))
                    recognizedPlates.Add(new PossiblePlate
                    {
                        ImgPlate = possiblePlate.ImgPlate,
                        StrChars = recognizedString,
                        RrLocationOfPlateInScene = possiblePlate.RrLocationOfPlateInScene
                    });
            }

            return recognizedPlates;
        }
    }
}
