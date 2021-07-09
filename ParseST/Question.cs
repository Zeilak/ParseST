using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;

namespace ParseST
{
    /// <summary>
    /// Тут хранится один вопрос из теста с ответами, правильными ответами, непроверенными ответами.
    /// Этот класс не только считывает весь вопрос со страницы, создает комбинации ответов, которые нужно проверить, но и вводит комбинацию ответов.
    /// </summary>
    /// <remarks>
    /// Возможные проблемы: одинаковые тексты вопросов (без картинки или с одинаковыми картинками).
    /// TODO: ул. хранение комбинации ответов, проверка для checkbox.
    /// </remarks>
    class Question
    {
        /// <value>Текст вопроса.</value>
        public string Quest { get; private set; }
        /// <value>Картинка вопроса (если она есть).</value>
        public string Img { get; private set; }
        /// <value>Тип вопроса: text, checkbox, radio.</value>
        public string Type { get; private set; }
        /// <value>Лист ответов.</value>
        public List<string> Answers { get; private set; } = new List<string>();
        /// <value>Проверенные комбинации ответов.</value>
        public List<List<string>> CheckAnswers { get; private set; } = new List<List<string>>();
        /// <value>Непроверенные комбинации ответов.</value>
        public List<List<string>> NotCheckAnswers { get; private set; } = new List<List<string>>();
        /// <value>Комбинация ответов с макс. очками.</value>
        public List<string> AnswerWithMaxScore { get; private set; }
        /// <value>Макс. кол-во очков.</value>
        public int MaxScore { get; private set; } = 0;
        /// <value>Показывает порядковый номер вопроса в последнем тесте (нужно для проверки результатов).</value>
        public int NumberInLastTest { get; private set; }
        /// <value>Показывает комбинацию ответов в последнем тесте(нужно для проверки результатов).</value>
        public List<string> AnswerInLastTest { get; private set; }
        /// <value>Макс.кол-во ответов для checkbox.</value>
        public int MaxNumberOfAnswers = 1;
        /// <value>Был ли этот вопрос в тесте (нужно для проверки резов).</value>
        public bool WasInLastTest = true;
        /// <summary>
        /// Обновляет maxscore, если нужно. Вызывается после проверки резов.
        /// </summary>
        /// <param name="newscore"></param>
        public void UpdateScore(int newscore)
        {
            if (Type != "text")
            {
                MaxScore = newscore;
                AnswerWithMaxScore = new List<string>(AnswerInLastTest);
            }

        }
        /// <summary>
        /// Конструктор нужно вызывать, если точно известно, что это новый вопрос.
        /// </summary>
        /// <param name="doc">страница с вопросом.</param>
        /// <param name="testForm">форма через которую отправляются ответы на вопросы. Форма не отправляется</param>
        /// <param name="questionNumber">номер вопроса в текущем тесте.</param>
        public Question(AngleSharp.Dom.IDocument doc, ref IHtmlFormElement testForm, int questionNumber)
        {

            Quest = doc.QuerySelector(".questionText").TextContent;

            var input = doc.QuerySelectorAll("form#questionForm input[type='checkbox']").FirstOrDefault();
            if (input != null)
            {
                Type = "checkbox";
            }
            else
            {
                input = doc.QuerySelectorAll("form#questionForm input[type='radio']").FirstOrDefault();
                if (input != null)
                {
                    Type = "radio";
                }
                else
                {
                    input = doc.QuerySelectorAll("form#questionForm input[type='text']").FirstOrDefault();
                    if (input != null)
                    {
                        Type = "text";
                    }
                    else
                    {
                        Console.WriteLine("Question конструктор: не определен тип вопроса.");
                    }
                }
            }
            if (Type == "checkbox")
            {
                string maxNumberOfAn = doc.QuerySelector("div.answers > script").TextContent;
                int[] intMatch = maxNumberOfAn.Where(Char.IsDigit).Select(x => int.Parse(x.ToString())).ToArray();
                MaxNumberOfAnswers = intMatch[0];
            }
            var answers = doc.QuerySelectorAll(".questionText");
            int u = 0;
            foreach (var answer in answers)
            {
                if (u != 0)
                    Answers.Add(answer.TextContent);
                u++;
            }
            var img = doc.QuerySelector(".questionText img");
            if (img != null)
            {
                Img = img.GetAttribute("src");
            }
            int m = Answers.Count, n = MaxNumberOfAnswers;
            CreateNotCheckAnswers(new List<string>(Answers), new List<string>(), n);
            DeleteDuplicAnswersCombination();
            CheckQuestion(doc, ref testForm, questionNumber);
        }
        /// <summary>
        /// Вставляет нужный комбинацию ответов в форму (не отправляет форму). Записывает эту комбинацию в AnswerInLastTest.
        /// Проверяет, остались ли еще непроверенные комбинации.
        /// </summary>
        /// <param name="doc">страница с вопросом.</param>
        /// <param name="testForm">форма через которую отправляются ответы на вопросы.</param>
        /// <param name="questionNumber">номер вопроса в текущем тесте.</param>
        /// <returns>true - есть еще непроверенные комбинации, false - их нет.</returns>
        public bool CheckQuestion(AngleSharp.Dom.IDocument doc, ref IHtmlFormElement testForm, int questionNumber)
        {
            bool ReturnValue = true;
            switch (Type)
            {

                case "text":
                    {
                        ReturnValue = false;
                        var queryInput = testForm.Elements["answer"] as IHtmlInputElement;
                        queryInput.Value = "a";
                        queryInput = testForm.Elements["questionNumber"] as IHtmlInputElement;
                        queryInput.Value = $"{questionNumber}";
                        NumberInLastTest = questionNumber;
                        WasInLastTest = true;
                    }
                    break;
                case "radio":
                    {
                        string nowCheckAnswer;
                        if (NotCheckAnswers.Count == 0)
                        {
                            ReturnValue = false;
                            nowCheckAnswer = AnswerInLastTest[0];


                        }
                        else
                        {
                            CheckAnswers.Add(NotCheckAnswers[0]);
                            nowCheckAnswer = NotCheckAnswers[0][0];
                            NotCheckAnswers.RemoveAt(0);
                        }
                        var answersInDoc = doc.QuerySelectorAll(".questionText");
                        int nowCheckAnswerNumber = -1, i = 0;
                        foreach (var answer in answersInDoc)
                        {
                            if (answer.TextContent == nowCheckAnswer)
                            {
                                nowCheckAnswerNumber = i - 1;
                            }
                            i++;
                        }
                        var queryInput = testForm.Elements["answer"] as IHtmlInputElement;
                        queryInput.Value = $"{nowCheckAnswerNumber}";
                        queryInput.IsChecked = true;
                        AnswerInLastTest = new List<string>();
                        AnswerInLastTest.Add(nowCheckAnswer);
                        var qInput = testForm.Elements["questionNumber"] as IHtmlInputElement;
                        qInput.Value = $"{questionNumber}";
                        NumberInLastTest = questionNumber;
                        WasInLastTest = true;

                    }
                    break;
                case "checkbox":
                    {
                        List<string> nowCheckAnswer;
                        if (NotCheckAnswers.Count == 0)
                        {
                            ReturnValue = false;
                            nowCheckAnswer = AnswerInLastTest;
                        }
                        else
                        {
                            CheckAnswers.Add(NotCheckAnswers[0]);
                            nowCheckAnswer = NotCheckAnswers[0];
                            NotCheckAnswers.RemoveAt(0);
                        }
                        var answersInDoc = doc.QuerySelectorAll(".questionText");
                        int i = 0;

                        foreach (var answer in answersInDoc)
                        {
                            foreach (var an in nowCheckAnswer)
                            {
                                if (answer.TextContent == an)
                                {
                                    var queryInput = testForm.Elements[$"{i - 1}"] as IHtmlInputElement;
                                    queryInput.IsChecked = true;
                                }

                            }
                            i++;
                        }
                        var qInput = testForm.Elements["questionNumber"] as IHtmlInputElement;
                        qInput.Value = $"{questionNumber}";
                        NumberInLastTest = questionNumber;
                        AnswerInLastTest = nowCheckAnswer;
                        WasInLastTest = true;
                    }
                    break;
            }
            ;
            return ReturnValue;
        }
        /// <summary>
        /// Создает все возможные комбинации ответов, нужно вызывать в конструкторе. Работает через рекурсию.
        /// TODO: Написать нормальный алгоритм
        /// </summary>
        /// <param name="answers">Список ответов</param>
        /// <param name="choicedAnswers"></param>
        /// <param name="maxNumber"></param>
        private void CreateNotCheckAnswers(List<string> answers, List<string> choicedAnswers, int maxNumber)
        {
            if (answers.Count == 0)
            {
                return;
            }


            for (int i = 0; i < answers.Count; i++)
            {
                var newCA = new List<string>(choicedAnswers);
                string an = answers[i];
                newCA.Add(an);
                var newA = new List<string>(answers);
                newA.RemoveAt(i);
                for (int ii = 1; ii < maxNumber; ii++)
                {
                    CreateNotCheckAnswers(new List<string>(newA), new List<string>(newCA), ii);
                }
                NotCheckAnswers.Add(newCA);
            }

        }
        /// <summary>
        /// костыль, нужно поменять алгоритм в CreateNotCheckAnswers
        /// </summary>
        private void DeleteDuplicAnswersCombination()
        {
            for (int i = 0; i < NotCheckAnswers.Count - 1; i++)
            {
                for (int j = i + 1; j < NotCheckAnswers.Count; j++)
                {
                    if (NotCheckAnswers[i].Count == NotCheckAnswers[j].Count)
                    {
                        int ok = 0;
                        int k = 0;
                        for (; k < NotCheckAnswers[i].Count; k++)
                        {
                            int kk = 0;
                            for (; kk < NotCheckAnswers[i].Count; kk++)
                            {
                                if (NotCheckAnswers[i][k] == NotCheckAnswers[j][kk])
                                {
                                    ok++;
                                }
                            }
                        }
                        if (ok == k)
                        {
                            NotCheckAnswers.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
        }
    }
}
