using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseLibrary.Entities
{
    public class CRDTTreeBuilder
    {
        StringBuilder actualText { get; set; }

        public CRDTTreeBuilder()
        {
            actualText = new StringBuilder();
        }

        public string BuildeTextFromCRDTDatabase(Dictionary<int, CRDTCharacter> crdtCharacters)
        {
            for(int i = 0; i < 10; i++)
            {
                if (crdtCharacters.ContainsKey(i))
                {
                   actualText.Append(BuildTree(i, crdtCharacters));
                }
                else
                {
                    break;
                }
            }
            return actualText.ToString();   
        }

        private string BuildTree(int currentId, Dictionary<int, CRDTCharacter> crdtCharacters)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (crdtCharacters.ContainsKey(currentId * 10 + i))
                {
                    BuildTree(currentId * 10 + i, crdtCharacters);
                }
                else
                {
                    break;
                }
            }
            actualText.Append(crdtCharacters[currentId].Character);
            for (int i = 5; i < 10; i++)
            {
                if (crdtCharacters.ContainsKey(currentId * 10 + i))
                {
                    BuildTree(currentId * 10 + i, crdtCharacters);
                }
            }
            return builder.ToString();
        }
    }
}