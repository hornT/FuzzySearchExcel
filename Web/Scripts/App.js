'use strict';

const logElement = document.querySelector('#log');

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
        reader.onload = onFileLoad;
        reader.readAsDataURL(file);
    };
});

function dropZoneOnClick() {
    $('#fileInput').trigger('click');
}

function fileChange(e) {

    var reader = new FileReader();
    var files = e.dataTransfer ? e.dataTransfer.files : e.target.files;

    reader.onload = onFileLoad;
    reader.readAsDataURL(files[0]);
}

/**
 * Загрузка файла
 * @param {any} e
 */
function onFileLoad(e) {
    const data = e.target.result;

    $.ajax({
        type: "POST",
        url: "/Home/UploadFile",
        data: { file: data },
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
 * 
 */
function ProcessFile() {
    const columnIndex = document.querySelector('#column').selectedIndex;
    if (columnIndex < 0) {
        AddLog('Не выбрана колонка');
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
                AddReplaceLog(res.ReplacementLog)
            }
        },
        error: function (q, w, e, r) {
            console.log('error');
            AddLog('Произошла ошика. Подробности в консоли');
        }
    });
}

var variantIndex = 0;
var possibleReplaces;

/**
 * 
 * @param {any} prepareResult
 */
function ShowVariants(prepareResult) {

    possibleReplaces = prepareResult.PossibleReplaces;

    variantIndex = possibleReplaces.length - 1;

    Next();
}

/**
 * 
 * @param {any} replacedValues
 */
function AddReplaceLog(replacedValues) {
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

    if (variantIndex < 0) {
        return;
    }

    const variantsCount = document.querySelector('#variantsCount');
    variantsCount.innerHTML = variantIndex + 1;

    const variantsChoose = document.querySelector('#variantsChoose');
    variantsChoose.innerHTML = '';

    const variants = possibleReplaces[variantIndex--];

    variants.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;
        option.value = ind;

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