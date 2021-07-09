using System;
using System.Collections.Generic;
using System.IO;
using AngleSharp.Html.Dom;

namespace ParseST
{
    /// <summary>
    /// Содержит ВСЕ вопросы (когда они все будут записаны, а это не обязательно одно прохождение теста). 
    /// Находит вопрос, который нужно проверить.
    /// Проверяет ответы в итоговой таблице теста.
    /// Записывает правильные ответы в файл.
    /// </summary>
    class QuestionsList
    {
        /// <value>Лист с вопросами.</value>
        private List<Question> _q = new List<Question>();
        /// <summary>
        /// Сохраняет правильные ответы в файл. Вопросы без найденных правильных ответов тоже сохраняются.
        /// </summary>
        /// <param name="writePath">Путь к файлу.</param>
        public void SaveResInFile(string writePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(writePath, false, System.Text.Encoding.Default))
                {
                    sw.WriteLine("Тут есть только правильные ответы.\n\n");
                    foreach (var q in _q)
                    {
                        if (q.AnswerWithMaxScore != null)
                        {
                            sw.WriteLine("-------------------------------------------------------");
                            sw.WriteLine(q.Quest);
                            if (q.Img != null)
                            {
                                sw.WriteLine("картинка: " + q.Img);
                            }
                            sw.WriteLine("-------------------------------------------------------");
                            foreach (var an in q.AnswerWithMaxScore)
                            {
                                sw.WriteLine(" + : " + an);
                            }
                            sw.WriteLine("Очки: " + q.MaxScore);
                            sw.WriteLine();
                            sw.WriteLine();
                        }

                    }
                    sw.WriteLine("\n\nК этим вопросам ответы не нашлись: \n\n");
                    foreach (var q in _q)
                    {
                        if (q.AnswerWithMaxScore == null)
                        {
                            sw.WriteLine("\n\n-------------------------------------------------------");
                            sw.WriteLine(q.Quest);
                            if (q.Img != null)
                            {
                                sw.WriteLine("картинка: " + q.Img);
                            }
                            sw.WriteLine("-------------------------------------------------------");
                            sw.WriteLine("тип ввода ответа: " + q.Type);
                        }

                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// Проверяет вопрос: находит нужный вопрос в списке вопросов и вызывает у него метод проверки.
        /// </summary>
        /// <param name="doc">страница с вопросом.</param>
        /// <param name="testForm">форма с ответами. Она не отправляется тут</param>
        /// <param name="questionNumber">номер вопроса в текущем тесте.</param>
        /// <returns></returns>
        public bool CheckQ(AngleSharp.Dom.IDocument doc, ref IHtmlFormElement testForm, int questionNumber)
        {
            string qDoc = doc.QuerySelector(".questionText").TextContent;
            foreach (var q in _q)
            {

                if (qDoc == q.Quest)
                {
                    var img = doc.QuerySelector(".questionText img");
                    if (img != null)
                    {
                        if (img.GetAttribute("src") == q.Img)
                        {
                            return q.CheckQuestion(doc, ref testForm, questionNumber);
                        }
                    }
                    else
                    {
                        return q.CheckQuestion(doc, ref testForm, questionNumber);
                    }
                }
            }
            _q.Add(new Question(doc, ref testForm, questionNumber));
            return true;
        }
        /// <summary>
        /// Ресетит переменные для начала проверки теста (вызывается перед проверкой первого вопроса в тесте).
        /// </summary>
        public void ConfirmStartTestParameters()
        {
            foreach (var q in _q)
            {
                q.WasInLastTest = false;
            }
        }
        /// <summary>
        /// Проверяет результаты, записывая их в сами вопросы (если нужно)
        /// </summary>
        /// <param name="resDoc">док с итоговой таблицей.</param>
        public void CheckResultTable(AngleSharp.Dom.IDocument resDoc)
        {
            var scores = resDoc.QuerySelectorAll(".score");
            int si = 0;
            foreach (var score in scores)
            {
                if (score.TextContent != "" && score.TextContent != "0")
                {
                    foreach (var q in _q)
                    {
                        if (q.NumberInLastTest == si && q.WasInLastTest && Int32.Parse(score.TextContent) > q.MaxScore)
                        {
                            q.UpdateScore(Int32.Parse(score.TextContent));
                        }
                    }
                }
                si++;
            }
        }
        
    }
}
