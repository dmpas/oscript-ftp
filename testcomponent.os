#Использовать ftp

// https://dlptest.com/ftp-test/
// Пользователь и пароль публичны
Соединение = Новый FtpСоединение("ftp.dlptest.com",, "dlpuser", "rNrKYTX9g7z3RgJRmxWuGHbeu");
Соединение.Записать("testcomponent.os", "/testcomponent.os");