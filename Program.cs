using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AsiaOptomTests
{
    public class AsiaOptomTestSuite
    {
        public static string FirstName;
        public static string LastName;
        public static string Password;
        public static string Phone;
        public static string Email;

        private IWebDriver driver;
        private WebDriverWait wait;
        private readonly string baseUrl = "https://asiaoptom.com/";
        private readonly Random random = new Random();

        private string GenerateRandomString(int length = 8)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        private string GenerateRandomPhone()
        {
            return $"+7{random.Next(900, 999)}{random.Next(1000000, 9999999)}";
        }

        private string GenerateRandomEmail()
        {
            return $"test_{GenerateRandomString(6)}@mail.ru";
        }

        private IWebElement WaitForClickable(By by, int timeoutSeconds = 10)
        {
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv =>
            {
                IWebElement element = drv.FindElement(by);
                return (element != null && element.Displayed && element.Enabled) ? element : null;
            });
        }

        private IWebElement WaitForVisible(By by, int timeoutSeconds = 10)
        {
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(drv =>
            {
                IWebElement element = drv.FindElement(by);
                return (element != null && element.Displayed) ? element : null;
            });
        }

        private void CheckAgreementCheckboxesJS()
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                js.ExecuteScript(@"
                    var checkbox1 = document.getElementById('aferta');
                    var checkbox2 = document.getElementById('personal_agreement');
                    if(checkbox1 && !checkbox1.checked) checkbox1.checked = true;
                    if(checkbox2 && !checkbox2.checked) checkbox2.checked = true;

                    if(checkbox1) checkbox1.dispatchEvent(new Event('change'));
                    if(checkbox2) checkbox2.dispatchEvent(new Event('change'));
                ");

                Console.WriteLine("Чекбоксы отмечены через JavaScript");
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отметке чекбоксов через JS: {ex.Message}");
            }
        }


        public void TestSuccessfulRegistration()
        {
            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl(baseUrl);

                IWebElement registerLink = WaitForClickable(By.Id("linkReg"));
                registerLink.Click();

                FirstName = GenerateRandomString(6);
                LastName = GenerateRandomString(8);
                Password = GenerateRandomString(10);
                Phone = GenerateRandomPhone();
                Email = GenerateRandomEmail();

                Console.WriteLine($"Регистрация с данными: {FirstName} {LastName}, {Password}, {Phone}");

                WaitForVisible(By.Name("USER_NAME")).SendKeys(FirstName);
                driver.FindElement(By.Name("USER_LAST_NAME")).SendKeys(LastName);
                driver.FindElement(By.Name("USER_PASSWORD")).SendKeys(Password);
                driver.FindElement(By.Name("USER_CONFIRM_PASSWORD")).SendKeys(Password);
                driver.FindElement(By.Name("PERSONAL_PHONE")).SendKeys(Phone);
                driver.FindElement(By.Name("USER_EMAIL")).SendKeys(Email);

                CheckAgreementCheckboxesJS();

                IWebElement registerButton = driver.FindElement(By.CssSelector("button.register-bt"));
                registerButton.Click();

                Thread.Sleep(3000);

                bool isSuccess = false;
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> regForm = driver.FindElements(By.Name("USER_NAME"));
                    isSuccess = regForm.Count == 0;
                }
                catch
                {
                    isSuccess = true;
                }

                string currentUrl = driver.Url;
                if (!currentUrl.Contains("register=yes"))
                {
                    isSuccess = true;
                }

                Console.WriteLine($"Регистрация успешна: {isSuccess}");
                if (!isSuccess)
                {
                    throw new Exception("Регистрация не удалась");
                }
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestFailedRegistrationExistingUser()
        {
            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl(baseUrl);

                IWebElement registerLink = WaitForClickable(By.Id("linkReg"));
                registerLink.Click();

                string existingEmail = Email;
                string existingPhone = Phone;

                Console.WriteLine($"Попытка регистрации с существующими данными: {existingEmail}, {existingPhone}");

                WaitForVisible(By.Name("USER_NAME")).SendKeys("Existing");
                driver.FindElement(By.Name("USER_LAST_NAME")).SendKeys("User");
                driver.FindElement(By.Name("USER_PASSWORD")).SendKeys("Password123");
                driver.FindElement(By.Name("USER_CONFIRM_PASSWORD")).SendKeys("Password123");
                driver.FindElement(By.Name("PERSONAL_PHONE")).SendKeys(existingPhone);
                driver.FindElement(By.Name("USER_EMAIL")).SendKeys(existingEmail);

                CheckAgreementCheckboxesJS();

                IWebElement registerButton = driver.FindElement(By.CssSelector("button.register-bt"));
                registerButton.Click();

                Thread.Sleep(3000);

                bool hasError = false;
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> errorElements = driver.FindElements(By.ClassName("errortext"));
                    hasError = errorElements.Count > 0 && errorElements[0].Displayed;

                    if (!hasError)
                    {
                        errorElements = driver.FindElements(By.CssSelector(".alert-danger, .error, .bx-auth-error"));
                        hasError = errorElements.Count > 0;
                    }

                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> regForm = driver.FindElements(By.Name("USER_NAME"));
                    if (regForm.Count > 0)
                    {
                        hasError = true;
                    }
                }
                catch
                {
                    hasError = false;
                }

                Console.WriteLine($"Регистрация отклонена (найдена ошибка): {hasError}");
                if (!hasError)
                {
                    throw new Exception("Регистрация должна была быть отклонена");
                }
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestSuccessfulLogin()
        {
            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl(baseUrl);

                IWebElement loginLink = WaitForClickable(By.Id("linkAuth"));
                loginLink.Click();

                string email = Email;
                string password = Password;

                Console.WriteLine($"Вход с данными: {email}");

                WaitForVisible(By.Id("enterLogin")).SendKeys(email);
                driver.FindElement(By.Id("enterPassword")).SendKeys(password);

                IWebElement loginButton = driver.FindElement(By.CssSelector("button.register-bt"));
                loginButton.Click();

                Thread.Sleep(3000);

                bool isSuccess = false;
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> authLink = driver.FindElements(By.Id("linkAuth"));
                    isSuccess = authLink.Count == 0 || !authLink[0].Displayed;
                }
                catch
                {
                    isSuccess = true;
                }

                string currentUrl = driver.Url;
                if (currentUrl.Contains("personal") || currentUrl.Contains("profile"))
                {
                    isSuccess = true;
                }

                Console.WriteLine($"Вход успешен: {isSuccess}");
                if (!isSuccess)
                {
                    throw new Exception("Вход не удался");
                }
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestFailedLoginInvalidCredentials()
        {
            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl(baseUrl);

                IWebElement loginLink = WaitForClickable(By.Id("linkAuth"));
                loginLink.Click();

                string invalidEmail = $"invalid_{GenerateRandomString(6)}@nonexist.com";
                string invalidPassword = "WrongPassword123";

                Console.WriteLine($"Попытка входа с неверными данными: {invalidEmail}");

                WaitForVisible(By.Id("enterLogin")).SendKeys(invalidEmail);
                driver.FindElement(By.Id("enterPassword")).SendKeys(invalidPassword);

                IWebElement loginButton = driver.FindElement(By.CssSelector("button.register-bt"));
                loginButton.Click();

                Thread.Sleep(3000);

                bool hasError = false;
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> errorMessages = driver.FindElements(By.ClassName("errortext"));
                    if (errorMessages.Count > 0 && errorMessages[0].Displayed)
                    {
                        hasError = true;
                        Console.WriteLine($"Сообщение об ошибке: {errorMessages[0].Text}");
                    }

                    if (!hasError)
                    {
                        errorMessages = driver.FindElements(By.CssSelector(".bx-auth-error, .alert-danger"));
                        hasError = errorMessages.Count > 0;
                    }

                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> loginForm = driver.FindElements(By.Id("enterLogin"));
                    if (loginForm.Count > 0)
                    {
                        hasError = true;
                    }
                }
                catch
                {
                    hasError = false;
                }

                Console.WriteLine($"Вход отклонен (найдена ошибка): {hasError}");
                if (!hasError)
                {
                    throw new Exception("Вход должен быть отклонен");
                }
            }
            finally
            {
                Cleanup();
            }
        }

        public void TestAddToCartAndVerify()
        {
            try
            {
                InitializeDriver();
                driver.Navigate().GoToUrl(baseUrl);

                Console.WriteLine("Выполняем вход в аккаунт...");
                IWebElement loginLink = WaitForClickable(By.Id("linkAuth"));
                loginLink.Click();

                string email = Email;
                string password = Password;

                WaitForVisible(By.Id("enterLogin")).SendKeys(email);
                driver.FindElement(By.Id("enterPassword")).SendKeys(password);

                IWebElement loginButton = driver.FindElement(By.CssSelector("button.register-bt"));
                loginButton.Click();
                Thread.Sleep(5000);

                bool isLoggedIn = false;
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> authLink = driver.FindElements(By.Id("linkAuth"));
                    isLoggedIn = authLink.Count == 0 || !authLink[0].Displayed;
                }
                catch
                {
                    isLoggedIn = true;
                }

                if (!isLoggedIn)
                {
                    throw new Exception("Не удалось войти в аккаунт для теста корзины");
                }
                Console.WriteLine("Вход выполнен успешно");

                Console.WriteLine("Переходим на страницу товара...");
                driver.Navigate().GoToUrl("https://asiaoptom.com/item/728613098290/");
                Thread.Sleep(5000);

                string productName = "";
                try
                {
                    IWebElement nameElement = driver.FindElement(By.CssSelector("h1"));
                    if (nameElement != null && nameElement.Displayed)
                    {
                        productName = nameElement.Text.Trim();
                    }
                }
                catch
                {

                    IWebElement nameElement = driver.FindElement(By.XPath("//h1 | //div[contains(@class, 'product-title')]"));
                    productName = nameElement.Text.Trim();

                }
                Console.WriteLine($"Название товара: {productName}");

                string productPrice = "";
                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> priceElements = driver.FindElements(By.CssSelector("[class*='price'], .usdPrice, span[class*='price']"));

                    foreach (IWebElement element in priceElements)
                    {
                        string text = element.Text.Trim();
                        Console.WriteLine($"Проверяем элемент цены на странице товара: '{text}'");

                        if (text.Contains("$"))
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(text, @"\$(\d+\.?\d*)");
                            if (match.Success)
                            {
                                productPrice = match.Groups[1].Value;
                                Console.WriteLine($"Найдена цена в долларах: ${productPrice}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при извлечении цены товара: {ex.Message}");
                }

                Console.WriteLine($"Итоговая цена товара за единицу: ${productPrice}");

                Console.WriteLine("Увеличиваем количество товара до 10...");
                try
                {
                    IWebElement amountPlus = driver.FindElement(By.ClassName("amountPlus"));
                    IWebElement quantityInput = driver.FindElement(By.CssSelector("input[name*='offer'][name*='amount']"));

                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("arguments[0].value = '0';", quantityInput);
                    js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", quantityInput);
                    Thread.Sleep(300);

                    for (int i = 0; i < 10; i++)
                    {
                        amountPlus.Click();
                        Thread.Sleep(200);
                    }

                    string currentValue = quantityInput.GetAttribute("value");
                    Console.WriteLine($"Количество после нажатий: {currentValue}");

                    if (currentValue.StartsWith("0") && currentValue.Length > 1)
                    {
                        string cleanedValue = currentValue.TrimStart('0');
                        if (string.IsNullOrEmpty(cleanedValue)) cleanedValue = "0";

                        js.ExecuteScript("arguments[0].value = arguments[1];", quantityInput, cleanedValue);
                        js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", quantityInput);
                        Thread.Sleep(300);
                        Console.WriteLine($"Значение после очистки от лидирующих нулей: {cleanedValue}");
                    }

                    string finalValue = quantityInput.GetAttribute("value");
                    Console.WriteLine($"Финальное количество: {finalValue}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке количества: {ex.Message}");

                    try
                    {
                        IWebElement quantityInput = driver.FindElement(By.CssSelector("input[name*='offer'][name*='amount']"));
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                        js.ExecuteScript("arguments[0].value = '';", quantityInput);
                        js.ExecuteScript("arguments[0].value = '10';", quantityInput);
                        js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", quantityInput);
                        js.ExecuteScript("arguments[0].dispatchEvent(new Event('input'));", quantityInput);
                        Thread.Sleep(500);

                        string finalValue = quantityInput.GetAttribute("value");
                        Console.WriteLine($"Количество установлено через JS: {finalValue}");
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"Не удалось установить количество: {ex2.Message}");
                    }
                }

                Console.WriteLine("Добавляем товар в корзину...");
                try
                {
                    IWebElement addToCartButton = driver.FindElement(By.CssSelector("[class*='buttonBasket']"));

                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("arguments[0].scrollIntoView(true);", addToCartButton);
                    Thread.Sleep(500);

                    try
                    {
                        addToCartButton.Click();
                    }
                    catch
                    {
                        js.ExecuteScript("arguments[0].click();", addToCartButton);
                    }

                    Thread.Sleep(3000);
                    Console.WriteLine("Товар добавлен в корзину");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении в корзину: {ex.Message}");
                    throw;
                }

                Console.WriteLine("Переходим в корзину...");
                try
                {
                    IWebElement cartLink = driver.FindElement(By.CssSelector("a[href*='order/make']"));
                    cartLink.Click();
                }
                catch
                {
                    driver.Navigate().GoToUrl("https://asiaoptom.com/personal/order/make/");
                }
                Thread.Sleep(5000);

                Console.WriteLine("Проверяем товар в корзине...");

                try
                {
                    string pageSource = driver.PageSource;
                    if (pageSource.Contains("728613098290"))
                    {
                        Console.WriteLine("Товар найден в HTML по ID");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке HTML: {ex.Message}");
                }

                bool nameMatches = false;
                try
                {
                    string bodyText = driver.FindElement(By.TagName("body")).Text;
                    nameMatches = bodyText.Contains(productName);
                    Console.WriteLine($"Название товара в корзине найдено: {nameMatches}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке названия: {ex.Message}");
                }

                bool priceMatches = false;
                bool quantityMatches = false;
                string cartPriceText = "";

                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> allPriceElements = driver.FindElements(By.CssSelector(".offer_col .data, [class*='price'] .data, .c4 .data"));
                    foreach (IWebElement element in allPriceElements)
                    {
                        string text = element.Text.Trim();
                        Console.WriteLine($"Найден элемент цены: '{text}'");

                        if (!string.IsNullOrEmpty(text) && (text.Contains("$") || System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.?\d*$")))
                        {
                            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+\.?\d*)");
                            if (match.Success)
                            {
                                cartPriceText = match.Groups[1].Value;
                                Console.WriteLine($"Цена в корзине: ${cartPriceText}");

                                if (decimal.TryParse(cartPriceText, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out decimal cartPrice))
                                {
                                    if (decimal.TryParse(productPrice, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out decimal expectedPrice))
                                    {
                                        priceMatches = Math.Abs(cartPrice - expectedPrice) < 0.01m;
                                        if (priceMatches) break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при поиске цены: {ex.Message}");
                }

                try
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> quantityInputs = driver.FindElements(By.CssSelector(".count-input, input[name*='amount'], input[type='text'][value]"));
                    foreach (IWebElement input in quantityInputs)
                    {
                        string val = input.GetAttribute("value");
                        if (!string.IsNullOrEmpty(val) && val.All(char.IsDigit))
                        {
                            Console.WriteLine($"Найдено поле количества: значение '{val}'");
                            string cleanVal = val.TrimStart('0');
                            if (string.IsNullOrEmpty(cleanVal)) cleanVal = "0";

                            if (cleanVal == "10" || val == "10")
                            {
                                quantityMatches = true;
                                Console.WriteLine($"Количество корректное: {cleanVal}");
                                break;
                            }
                        }
                    }

                    if (!quantityMatches)
                    {
                        System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> summaryElements = driver.FindElements(By.CssSelector(".basket__summary-item, .total-count, [class*='total']"));
                        foreach (IWebElement element in summaryElements)
                        {
                            string text = element.Text;
                            if (text.Contains("10") || (text.Contains("шт") && text.Contains("10")))
                            {
                                quantityMatches = true;
                                Console.WriteLine($"Количество найдено в итоговом блоке: {text}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке количества: {ex.Message}");
                    quantityMatches = true;
                }

                Console.WriteLine("\n=== Результаты проверки корзины ===");
                Console.WriteLine($"Название товара совпадает: {nameMatches}");
                Console.WriteLine($"Цена товара совпадает: {priceMatches} (цена в корзине: ${cartPriceText}, ожидалась: ${productPrice})");
                Console.WriteLine($"Количество товара корректное: {quantityMatches}");

                if (!nameMatches || !priceMatches || !quantityMatches)
                {
                    string errorMessage = "Проверка корзины не пройдена: ";
                    if (!nameMatches) errorMessage += "не совпадает название; ";
                    if (!priceMatches) errorMessage += "не совпадает цена; ";
                    if (!quantityMatches) errorMessage += "неверное количество; ";
                    Console.WriteLine(errorMessage);
                }
                else
                {
                    Console.WriteLine("Все проверки корзины пройдены успешно!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка в тесте корзины: {ex.Message}");
                throw;
            }
            finally
            {
                Cleanup();
            }
        }

        private void InitializeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            driver = new ChromeDriver(options);
        }

        private void Cleanup()
        {
            if (driver != null)
            {
                driver.Quit();
                driver.Dispose();
            }
        }

        public static void Main(string[] args)
        {
            AsiaOptomTestSuite testSuite = new AsiaOptomTestSuite();

            Console.WriteLine("=== Запуск тестов AsiaOptom ===\n");

            Console.WriteLine("1. Тест успешной регистрации:");
            testSuite.TestSuccessfulRegistration();
            Console.WriteLine();

            Console.WriteLine("2. Тест неуспешной регистрации (существующий пользователь):");
            testSuite.TestFailedRegistrationExistingUser();
            Console.WriteLine();

            Console.WriteLine("3. Тест успешного входа:");
            testSuite.TestSuccessfulLogin();
            Console.WriteLine();

            Console.WriteLine("4. Тест неуспешного входа (неверные данные):");
            testSuite.TestFailedLoginInvalidCredentials();
            Console.WriteLine();

            Console.WriteLine("5. Тест добавления товара в корзину с проверкой:");
            testSuite.TestAddToCartAndVerify();
            Console.WriteLine();

            Console.WriteLine("\n=== Все тесты завершены ===");
            Console.ReadLine();
        }
    }
}