using System;
using AngleSharp;
using AngleSharp.Html.Dom;

namespace ParseST
{
    /// <summary>
    /// Содержит основную логику нахождения ответов.
    /// </summary>
    class ParserAnswersST
    {
        /// <summary>
        /// Находит ответы на вопросы в тесте с открытой репетицией.
        /// </summary>
        /// <param name="theme_name">id теста</param>
        /// <param name="filename">путь к файлу для записи результатов.</param>
        public static async void GetAnswersDefault(string theme_name, string filename)
        {
            // тут хранятся вопросы с ответами
            QuestionsList qList = new QuestionsList();

            // создаем подключение и открывем сайт
            bool haveNotCheckedAnswersComb = true;
            int testCounter = 0;
            var config = Configuration.Default
                    .WithDefaultLoader()
                    .WithDefaultCookies();
            var address = "http://scientia-test.com/forstudent/";
            Console.WriteLine("Loading http://scientia-test.com/forstudent/");
            var contex = BrowsingContext.New(config);

            // цикл проверки тестов: одна итерация - один полный тест
            // заканчивается, когда не осталось непроверенных комбинаций ответов.
            // другими словами: продолжается, пока есть непроверенные ответы
            Console.WriteLine("Loading test...");
            while (haveNotCheckedAnswersComb)
            {
                // открытие и старт теста
                qList.ConfirmStartTestParameters(); // это нужно делать перед началом каждого теста
                Console.WriteLine("Searching for answers..." + $"{testCounter}");
                testCounter++;
                haveNotCheckedAnswersComb = false;
                var document = await contex.OpenAsync(address);
                var formDoc = document.QuerySelector("form#testLoginForm") as IHtmlFormElement;
                formDoc.Action = "\\trial\\";
                var resultDocument = await formDoc.SubmitAsync(new { theme = theme_name });
                if (resultDocument.QuerySelector(".questionText") == null)
                {
                    Console.WriteLine("Error: test did not load");
                }

                // цикл по вопросам
                // продолжается, пока не открылась таблица с результатами
                int questionCounter = 0;
                while (resultDocument.QuerySelector(".resultsTable") == null)
                {
                    var testForm = resultDocument.QuerySelector("form#questionForm") as IHtmlFormElement;
                    if (qList.CheckQ(resultDocument, ref testForm, questionCounter)) haveNotCheckedAnswersComb = true;
                    resultDocument = await testForm.SubmitAsync(); // уже составленная форма отправляется тут
                    questionCounter++;
                }
                qList.CheckResultTable(resultDocument);

            }
            Console.WriteLine("Save results in " + filename);
            qList.SaveResInFile(filename);
            Console.WriteLine("End.");
        }
    }
}
