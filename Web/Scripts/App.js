'use strict';

const logElement = document.querySelector('#log');

var variantIndex = 0;
var possibleReplaces;

$(document).ready(function () {
    var dropZone = $('#dropZone');

    if (typeof (window.FileReader) === 'undefined') {
        dropZone.text('Не поддерживается браузером!');
        dropZone.addClass('error');
    }

    dropZone[0].ondragover = function () {
        dropZone.addClass('hover');
        return false;
    };

    dropZone[0].ondragleave = function () {
        dropZone.removeClass('hover');
        return false;
    };

    dropZone[0].ondrop = function (event) {
        event.preventDefault();
        dropZone.removeClass('hover');
        dropZone.addClass('drop');

        const file = event.dataTransfer.files[0];
        const reader = new FileReader();
        //reader.onload = onFileLoad;
        reader.onload = function (e) {
            onFileLoad(e, file.name);
        };
        reader.readAsDataURL(file);
    };
});

function dropZoneOnClick() {
    $('#fileInput').trigger('click');
}

function fileChange(e) {

    var reader = new FileReader();
    var files = e.dataTransfer ? e.dataTransfer.files : e.target.files;

    reader.onload = function (e) {
        onFileLoad(e, files[0].name);
    };
    reader.readAsDataURL(files[0]);
}

/**
 * Загрузка файла
 * @param {any} e
 */
function onFileLoad(e, fileName) {
    const data = e.target.result;

    $.ajax({
        type: "POST",
        url: "/Home/UploadFile",
        data: { file: data, fileName: fileName },
        success: function (res) {
            AddLog(res.message);
            if (res.columns)
                InitColumns(res.columns);
        },
        error: function (q, w, e, r) {
            console.log('error');
            AddLog('Произошла ошика. Подробности в консоли');
        }
    });
}

/**
 * Проинициализировать контрол с колонками
 * @param {any} columns
 */
function InitColumns(columns) {
    const columnSelect = document.querySelector('#column');
    columnSelect.innerHTML = '';

    columns.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;
        option.value = ind;

        columnSelect.appendChild(option);
    });
}

/**
 * Обработать файл
 */
function ProcessFile() {
    const columnIndex = document.querySelector('#column').selectedIndex;
    if (columnIndex < 0) {
        AddLog('Не выбрана колонка'); // TODO error
        return;
    }

    $.ajax({
        type: "POST",
        url: "/Home/ProcessFile",
        data: { columnIndex: columnIndex },
        success: function (res) {
            AddLog(res.message);
            if (res.prepareResult) {
                ShowVariants(res.prepareResult);
                AddReplaceLog(res.prepareResult.ReplacementLog)
            }
        },
        error: function (q, w, e, r) {
            console.log('error');
            AddLog('Произошла ошика. Подробности в консоли'); // TODO error
        }
    });
}

/**
 * Отобразить варианты замены
 * @param {any} prepareResult
 */
function ShowVariants(prepareResult) {

    possibleReplaces = prepareResult.PossibleReplaces;

    variantIndex = possibleReplaces.length - 1;

    Next();
}

/**
 * Отобразить лог замены
 * @param {any} replacedValues
 */
function AddReplaceLog(replacedValues) {
    if (!replacedValues)
        return;

    AddLog('Произведена автозамена:');
    replacedValues.forEach(elem => {
        AddLog(elem);
    });
}

/**
 * 
 */
function DownloadFile() {

}

/**
 * Выбор следующего набора возможных вариантов
 */
function Next() {

    const variantsChoose = document.querySelector('#variantsChoose');
    variantsChoose.innerHTML = '';

    if (variantIndex < 0) {
        return;
    }

    const variantsCount = document.querySelector('#variantsCount');
    variantsCount.innerHTML = variantIndex + 1;

    const variants = possibleReplaces[variantIndex--];

    variants.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;
        option.value = elem;

        variantsChoose.appendChild(option);
    });
}

/**
 * Удалить выбранное название компании
 */
function Remove() {

    $("#variantsChoose option:selected").remove();
}


/**
 * Добавить названия компаний в базу
 */
function AddValue() {
    const variantsChoose = document.querySelector('#variantsChoose');
    if (variantsChoose.selectedIndex < 0) {
        AddLog('Не выбран вариант'); // TODO error
        return;
    }

    const options = $('#variantsChoose option');
    const values = $.map(options, v => v.value);
    const selectedValue = variantsChoose.selectedOptions[0].value;

    $.ajax({
        type: "POST",
        url: "/Home/AddCompany",
        data: { values: values, keyWord: selectedValue },
        success: function (res) {
            //AddLog(res.message);
        },
        error: function (q, w, e, r) {
            console.log('error');
            AddLog('Произошла ошика. Подробности в консоли'); // TODO error
        }
    });

    Next();
}

/**
 * Добавление лога
 * @param {any} text
 */
function AddLog(text) {

    const textLog = document.createElement('div');
    textLog.classList.add('text-log');
    textLog.innerHTML = text;

    logElement.appendChild(textLog);
}