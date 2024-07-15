using System;
using System.IO;

namespace ConsoleApp
{
    internal class Program
    {
        private static byte[,] _sBox = {
            { 0xC, 0x4, 0x6, 0x2, 0xA, 0x5, 0xB, 0x9, 0xE, 0x8, 0xD, 0x7, 0x0, 0x3, 0xF, 0x1 },
            { 0x6, 0x8, 0x2, 0x3, 0x9, 0xA, 0x5, 0xC, 0x1, 0xE, 0x4, 0x7, 0xB, 0xD, 0x0, 0xF },
            { 0xB, 0x3, 0x5, 0x8, 0x2, 0xF, 0xA, 0xD, 0xE, 0x1, 0x7, 0x4, 0xC, 0x9, 0x6, 0x0 },
            { 0xC, 0x8, 0x2, 0x1, 0xD, 0x4, 0xF, 0x6, 0x7, 0x0, 0xA, 0x5, 0x3, 0xE, 0x9, 0xB },
            { 0x7, 0xF, 0x5, 0xA, 0x8, 0x1, 0x6, 0xD, 0x0, 0x9, 0x3, 0xE, 0xB, 0x4, 0x2, 0xC },
            { 0x5, 0xD, 0xF, 0x6, 0x9, 0x2, 0xC, 0xA, 0xB, 0x7, 0x8, 0x1, 0x4, 0x3, 0xE, 0x0 },
            { 0x8, 0xE, 0x2, 0x5, 0x6, 0x9, 0x1, 0xC, 0xF, 0x4, 0xB, 0x0, 0xD, 0xA, 0x3, 0x7 },
            { 0x1, 0x7, 0xE, 0xD, 0x0, 0x5, 0x8, 0x3, 0x4, 0xF, 0xA, 0x6, 0x9, 0xC, 0xB, 0x2 },
        };

        private const uint C1 = 0x10000202; // 2^24 + 2^16 + 2^8 + 2^2
        private const uint C2 = 0x10000101; // 2^24 + 2^16 + 2^8 + 1

        public static void Main(string[] args)
        {
            byte[] key = {
                 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
            };

            byte[] sync = {
    0b00010000, 0b00010001, 0b00010010, 0b00010011,
    0b00010100, 0b00010101, 0b0001011010, 0b00010111
};

            //byte[] sync = {
            //     0x10, 0x11, 0x12, 0x13,
            //     0x14, 0x15, 0x16, 0x17

            //};

            EncryptFile("plain.txt", "encryption.txt", key, sync);
            Console.WriteLine("Текст успешно зашифрован");

            DecryptFile("encryption.txt", "decryption.txt", key, sync);
            Console.WriteLine("Текст успешно расшифрован");
        }

        private static void EncryptFile(string srcFileName, string dstFileName, byte[] key, byte[] sync)
        {
            // Открываем файл для чтения
            using var binaryReader = new BinaryReader(File.OpenRead(srcFileName));
            // Открываем файл для записи
            using var binaryWriter = new BinaryWriter(File.Open(dstFileName, FileMode.Create));

            // Генерируем гамму на основе синхропосылки
            uint[] gamma = GenerateGamma(sync, (int)Math.Ceiling((double)binaryReader.BaseStream.Length / key.Length), key);
            // Буфер для считывания данных блока размером 8 байт
            byte[] buffer = new byte[8]; // 64-bit buffer
            int bytesRead;

            int gammaIndex = 0;

            // Считываем блоки данных из файла 
            while ((bytesRead = binaryReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Применяем гамму к блоку данных
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Применяем операцию XOR к каждому байту блока с соответствующим байтом гаммы
                    buffer[i] ^= (byte)(gamma[gammaIndex] >> (i * 8)); // XOR with gamma
                }

                // Записываем только реально считанные байты в файл
                binaryWriter.Write(buffer, 0, bytesRead);

                // Обновляем индекс гаммы для следующей итерации
                gammaIndex = (gammaIndex + 1) % gamma.Length;
            }
        }

        private static void DecryptFile(string srcFileName, string dstFileName, byte[] key, byte[] sync)
        {
            // Процесс расшифровки аналогичен процессу шифрования, но с обратной гаммой
            using var binaryReader = new BinaryReader(File.OpenRead(srcFileName));
            using var binaryWriter = new BinaryWriter(File.Open(dstFileName, FileMode.Create));

            // Генерируем гамму на основе синхропосылки
            uint[] gamma = GenerateGamma(sync, (int)Math.Ceiling((double)binaryReader.BaseStream.Length / key.Length), key);
            // Буфер для считывания данных блока размером 8 байт
            byte[] buffer = new byte[8]; // 64-bit buffer
            int bytesRead;

            int gammaIndex = 0;

            // Считываем блоки данных из файла и расшифровываем их
            while ((bytesRead = binaryReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Применяем гамму к блоку данных
                for (int i = 0; i < bytesRead; i++)
                {
                    // Применяем операцию XOR к каждому байту блока с соответствующим байтом гаммы
                    buffer[i] ^= (byte)(gamma[gammaIndex] >> (i * 8)); // XOR with gamma
                }

                // Записываем только реально считанные байты в файл
                binaryWriter.Write(buffer, 0, bytesRead);

                // Обновляем индекс гаммы для следующей итерации
                gammaIndex = (gammaIndex + 1) % gamma.Length;
            }
        }

        private static uint[] GenerateGamma(byte[] S, int m, byte[] key)
        {
            // Создаем массив для хранения гаммы длиной m
            uint[] gamma = new uint[m];
            // Извлекаем значения Y и Z из синхропосылки S
            uint Y = BitConverter.ToUInt32(S, 0);
            uint Z = BitConverter.ToUInt32(S, 4);
            // Генерируем m элементов гаммы
            for (int i = 0; i < m; i++)
            {
                // Шифруем текущий блок Y и сохраняем результат во временной переменной tmp
                uint tmp = EncodeBlock(Y, key, false);
                // Производим операцию {+} над Y с константой C2 и обновляем значение Y
                Y = (Y + C2);
                // Производим операцию {+} над Z с константой C1 и обновляем значение Z
                Z = (Z + C1 - 1) % uint.MaxValue + 1;
                // Сохраняем временное значение tmp в текущий элемент гаммы
                gamma[i] = tmp;
            }
            // Возвращаем массив гаммы
            return gamma;
        }

        private static uint EncodeBlock(uint block, byte[] key, bool isDecrypt)
        {
            // Извлекаем левую часть блока (16 бит)
            uint Y = (block >> 16) & 0xFFFF; // Левая часть блока
                                             // Извлекаем правую часть блока (16 бит)
            uint Z = block & 0xFFFF;          // Правая часть блока
                                              // Вычисляем ключи для сети Фейстеля
            uint[] keys = ScheduleKeys(key, isDecrypt);

            // Выполняем 32 раунда шифрования/расшифрования
            for (int i = 0; i < 32; i++)
            {
                // Вычисляем новую правую часть блока путем применения функции F к текущей правой части
                uint newRightPart = Y ^ CalculateF(Z, keys[i]); // Вызываем CalculateF для правой части
                                                                // Обновляем значения Y и Z для следующего раунда
                Y = Z;
                Z = newRightPart;
            }

            // Объединяем левую и правую части блока (левая << 16 | правая) для возврата результата
            return Z << 16 | Y; // Переставляем части перед объединением
        }

        private static uint CalculateF(uint rightPart, uint key)
        {

            rightPart += key;
            // Применяем простую функцию замены, используя S-Box
            for (int i = 0; i < 8; i++)
            {
                // Извлекаем значение байта из правой части блока и применяем к нему S-Box
                byte value = (byte)((rightPart >> (4 * (7 - i))) & 0xF);
                value = _sBox[i, value];
                // Очищаем старшие 4 бита текущего байта и заменяем их на новые значения из S-Box
                rightPart &= ~(uint)(0xF << (4 * (7 - i)));
                rightPart |= (uint)(value << (4 * (7 - i)));
            }

            // Применяем циклический сдвиг влево на 11 битов
            rightPart = CircularLeftShift(rightPart, 11);

            return rightPart;
        }


        private static uint CircularLeftShift(uint value, int shiftValue)
        {
            // Выполняем циклический сдвиг влево на заданное количество битов
            // Перемещаем биты влево на `shiftValue` позиций и добавляем к ним биты, вытесненные слева
            return (value << shiftValue) | (value >> (32 - shiftValue));
        }

        private static uint[] ScheduleKeys(byte[] sourceKey, bool isDecrypt = false)
        {
            // Преобразуем исходный ключ из массива байтов в массив беззнаковых 32-битных целых чисел
            uint[] sourceKeys = CombineByteKeyToUInt32Key(sourceKey);

            // Создаем массив для хранения ключей
            uint[] resultKeys = new uint[32];

            // Заполняем первые 24 ключа значениями из sourceKeys, повторяя их
            for (int i = 0; i < 24; i++)
                resultKeys[i] = sourceKeys[i % 8];

            // Заполняем оставшиеся 8 ключей значениями из sourceKeys в обратном порядке
            for (int i = 0; i < 8; i++)
                resultKeys[31 - i] = sourceKeys[i];

            // Если происходит процесс дешифрования, меняем порядок ключей на обратный
            if (isDecrypt)
                Array.Reverse(resultKeys);

            return resultKeys; // Возвращаем массив сгенерированных ключей
        }


        private static uint[] CombineByteKeyToUInt32Key(byte[] key)
        {
            // Создаем массив для хранения 32-битных ключей
            uint[] newKey = new uint[key.Length / 4];

            // Индекс для отслеживания текущего элемента в массиве байтов ключа
            int keyElementIndex = 0;

            // Проходим по новому массиву ключей
            for (int i = 0; i < newKey.Length; i++)
            {
                // Конвертируем четыре последовательных байта в беззнаковое 32-битное целое число
                newKey[i] = CombineBytesToUInt32(
                    key[keyElementIndex++], // Первый байт
                    key[keyElementIndex++], // Второй байт
                    key[keyElementIndex++], // Третий байт
                    key[keyElementIndex++]  // Четвертый байт
                );
            }

            return newKey; // Возвращаем массив с новыми беззнаковыми 32-битными ключами
        }


        private static uint CombineBytesToUInt32(byte first, byte second, byte third, byte fourth)
        {
            // Создаем 32-битное беззнаковое целое число, объединяя четыре байта
            return (uint)first << 24   // Сдвигаем байт влево на 24 бита, чтобы он стал самым старшим байтом
                 | (uint)second << 16  // Сдвигаем байт влево на 16 бит, чтобы он стал вторым по старшинству байтом
                 | (uint)third << 8    // Сдвигаем байт влево на 8 бит, чтобы он стал третьим по старшинству байтом
                 | (uint)fourth;       // Оставляем четвертый байт без изменений
        }

    }
}
