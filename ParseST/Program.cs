using System;

namespace ParseST
{
    class Program
    {
        static void Main(string[] args)
        {
            //new ParserAnswersST().GetAnswers("theme_ekgrig_252", "test1.txt");
            //new ParserAnswersST().GetAnswers("theme_ekgrig_447", "test2.txt");
            //new ParserAnswersST().GetAnswers("theme_ekgrig_700", "test3.txt");
            ParserAnswersST.GetAnswersDefault("theme_5ee3a5e0ca968_918", "osans3.txt");
            Console.ReadKey();
        }
    }

}