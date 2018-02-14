'use strict';

const logElement = document.querySelector('#log');
const baseNameInp = document.querySelector('#baseNameInp');
const baseNameLnk = document.querySelector('#baseNameLnk');
const variantsChoose = document.querySelector('#variantsChoose');
const variantsCount = document.querySelector('#variantsCount');
const variantsTotalCount = document.querySelector('#variantsTotalCount');
const columnSelect = document.querySelector('#column');
const possibleBaseNamesData = document.querySelector('#possibleBaseNames');
const overlap = $('#overlapLoader');
const popupPrepareResults = $('#prepareResults');
const popupPrepareResultsList = document.querySelector('#prepareResultsList');
const popupExclusionsWindow = $('#exclusionsWindow');

/**
 * Список, в котором отобра
 */
const popupExclusionsList = document.querySelector('#exclusionsList');

var variantIndex = 0;
var possibleReplaces;
var baseNames;

/**
 * Исключенные варианты
 */
let exclusions = [];

/**
 * Необработанные варианты
 */
let unworkedNames = [];

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

function log(text) {
    console.log(text);
}

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
 * @param {any} fileName
 */
function onFileLoad(e, fileName) {
    const data = e.target.result;

    overlap.show();

    $.ajax({
        type: "POST",
        url: "/Home/UploadFile",
        data: { file: data, fileName: fileName },
        success: function (res) {
            AddLog(res.message);
            if (res.columns)
                InitColumns(res.columns);

            // Сбросить значение файла
            document.getElementById("fileInput").value = "";
        },
        error: function (q, w, e, r) {
            console.log('error');
            AddLog('Произошла ошика. Подробности в консоли');
        },
        complete: function () {
            overlap.hide();
        }
    });
}

/**
 * Залогировать ошибку запроса
 * @param {any} xhr
 * @param {any} status
 * @param {any} error
 */
function AjaxErrorLog(xhr, status, error) {
    console.error(`Произошла ошибка. xhr: ${xhr}, status: ${status}, error: ${error}`);
    AddLog('Произошла ошика. Подробности в консоли');
}

/**
 * Проинициализировать контрол с колонками
 * @param {any} columns
 */
function InitColumns(columns) {
    
    columnSelect.innerHTML = '';

    columns.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;
        option.value = ind;

        columnSelect.appendChild(option);
    });

    // Разблокировать кнопку "Начать обработку"
    $('#btnProcessFile').prop('disabled', false);
}

/**
 * Обработать файл и получить предварительный результат замен
 */
function ProcessFile() {

    const columnIndex = columnSelect.selectedIndex;
    if (columnIndex < 0) {
        AddLog('Не выбрана колонка');
        return;
    }

    overlap.show();

    const selectedColumn = columnSelect.selectedOptions[0].text;

    $.ajax({
        type: "POST",
        url: "/Home/ProcessFile",
        data: { columnName: selectedColumn },
        success: function (res) {
            AddLog(res.message);
            if (res.prepareResult) {
                ShowVariants(res.prepareResult);
            }
        },
        error: AjaxErrorLog,
        complete: function () {
            overlap.hide();
        }
    });
}

/**
 * Заполнить список базовых имен
 * @param {any} baseNames
 */
function FillBaseNames(baseNames) {

    possibleBaseNamesData.innerHTML = '';
    baseNames.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;

        possibleBaseNamesData.appendChild(option);
    });
}

/**
 * Отобразить варианты замены
 * @param {any} prepareResult
 */
function ShowVariants(prepareResult) {

    possibleReplaces = prepareResult.PossibleReplaces;
    baseNames = prepareResult.BaseNames;
    unworkedNames = prepareResult.UnworkedNames;

    variantIndex = -1;
    variantsTotalCount.innerHTML = possibleReplaces.length;

    Next();

    AddReplaceLog(prepareResult.ReplacementLog);

    // Запомнить базовые названия
    FillBaseNames(baseNames);

    // Разблокировать кнопки
    $('#btnNext').prop('disabled', false); // Следующий
    $('#btnPrepareResult').prop('disabled', false); // Посмотреть результат
    $('#btnDownLoadFile').removeClass('disabled'); // Скачать
    $('#btnExclusion').prop('disabled', false); // Необработанные
    $('#btnDeleteBaseName').prop('disabled', false); // Удалить базовое наименование

    exclusions = [];
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
 * Выбор следующего набора возможных вариантов
 */
function Next() {

    log(`Next. variantIndex: ${variantIndex}, possibleReplaces.length: ${possibleReplaces.length}`);

    variantsChoose.innerHTML = '';
    baseNameInp.value = '';

    if (variantIndex >= (possibleReplaces.length - 1) || possibleReplaces.length === 0)
        return;

    variantIndex++;

    // Разблокировать кнопку "предыдущий"
    if (variantIndex > 0)
        $('#btnPrevious').prop('disabled', false);

    FillCurrentVariantSet();

    if (variantIndex >= possibleReplaces.length - 1)
        $('#btnNext').prop('disabled', true);
}

/**
 * Выбор предыдущего набора возможных вариантов
 */
function Previous() {
    variantsChoose.innerHTML = '';
    baseNameInp.value = '';

    if (variantIndex <= 0)
        return;

    // Разблокировать кнопку "следующий"
    $('#btnNext').prop('disabled', false);

    variantIndex--;

    FillCurrentVariantSet();

    if (variantIndex <= 0)
        $('#btnPrevious').prop('disabled', true);
}

/**
 * Заполнить набор вариантов для замены
 */
function FillCurrentVariantSet() {

    variantsCount.innerHTML = variantIndex + 1;

    const variants = possibleReplaces[variantIndex];

    // Заблокировать/разблокировать кнопки исключить и добавить
    const isValuesExists = variants.Values.length > 0;
    $('#btnExclude').prop('disabled', isValuesExists === false); // Исключить
    $('#btnAddValue').prop('disabled', isValuesExists === false); // Добавить

    FillVariantsWindow(variants.Values, variants.BaseName);
}

/**
 * Заполнить окно с вариантами замен
 * @param {any} variants
 * @param {any} baseName
 */
function FillVariantsWindow(variants, baseName) {

    variantsChoose.innerHTML = '';
    baseNameLnk.innerHTML = baseName || '';

    variants.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;
        option.value = elem;

        variantsChoose.appendChild(option);
    });
}

/**
 * Клик по базовому наименованию
 * @param {any} event
 */
function BaseNameClick(event) {

    baseNameInp.value = baseNameLnk.text;
}

/**
 * Клик по одному из предложенных вариантов
 * @param {any} event
 */
function VariantClick(event) {

    const selectedValue = variantsChoose.selectedOptions[0];
    if (selectedValue)
        baseNameInp.value = selectedValue.value;
}

/**
 * Удалить выбранное название компании
 */
function Exclude() {

    const selectedValue = $("#variantsChoose option:selected");

    // Добавить этот вариант в список исключенных для дальнейшей обработки
    const bsdata = popupExclusionsWindow.data('bs.modal');
    if (bsdata && bsdata.isShown !== true) {
        exclusions.push(selectedValue[0].value);
    }

    selectedValue.remove();

    // Заблокировать/разблокировать кнопки исключить и добавить
    // TODO
    //variantsChoose
    //$('#btnExclude').prop('disabled', isValuesExists); // Исключить
    //$('#btnAddValue').prop('disabled', isValuesExists); // Добавить
}

/**
 * Добавить названия компаний в базу
 */
function AddValue() {

    const selectedValue = baseNameInp.value;

    if (!selectedValue) {
        AddLog('Не выбран вариант'); // TODO error
        return;
    }

    const values = $.map(variantsChoose.options, v => v.value);

    overlap.show();

    $.ajax({
        type: "POST",
        url: "/Home/AddCompany",
        data: { values: values, keyWord: selectedValue },
        success: function (res) {
            // Запомнить базовые названия
            FillBaseNames(res.baseNames);

            // Помечаем текущий вариант как обработанный
            const bsdata = popupExclusionsWindow.data('bs.modal');
            if (bsdata && bsdata.isShown === true)
                return;

            const variantSet = possibleReplaces[variantIndex];
            variantSet.IsProcessed = true;

            Next();
        },
        error: AjaxErrorLog,
        complete: function () {
            overlap.hide();
        }
    });
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

/**
 * Получить предварительный результат
 */
function GetPrepareResult() {
    overlap.show();

    $.ajax({
        type: "GET",
        url: "/Home/GetPrepareResult",
        success: function (res) {
            ShowPrepareResult(res.values);
        },
        error: AjaxErrorLog,
        complete: function () {
            overlap.hide();
        }
    });
}

/**
 * Показать предварительный результат
 */
function ShowPrepareResult(values) {

    popupPrepareResultsList.innerHTML = '';

    values.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;

        popupPrepareResultsList.appendChild(option);
    });

    popupPrepareResults.modal('show');
}

/**
 * Скрыть предварительный результат
 * @param {any} e
 */
function HidePrepareResult(e) {

    if (e.target.tagName !== 'DIV') {
        e.preventDefault();
        return;
    }

    popupPrepareResults.modal({ show: false });
}

/**
 * Показать окно с необработанными вариантами
 */
function ShowExclusionsWindow() {

    popupExclusionsList.innerHTML = '';

    const elements = possibleReplaces
        .filter(el => el.IsProcessed !== true)
        .map(el => el.Values);

    const exclusionValues = [].concat.apply([], elements)
        .concat(exclusions)
        .concat(unworkedNames)
        .sort();

    exclusionValues.forEach((elem, ind) => {
        const option = document.createElement('option');
        option.innerHTML = elem;

        popupExclusionsList.appendChild(option);
    });

    popupExclusionsWindow.modal({ backdrop: false });
    // Хак https://stackoverflow.com/questions/8168746/bootstrap-css-how-to-make-a-modal-dialog-modal-without-affecting-the-backgroun
    //$('.modal-backdrop').removeClass("modal-backdrop");
}

/**
 * Переместить выбранные исключенные наименования для дальнейшей обработки
 */
function MoveFromExcludeToVariants() {

    const selectedOptions = popupExclusionsList.selectedOptions;
    const selectedValues = $.map(selectedOptions, v => v.value);

    FillVariantsWindow(selectedValues);

    const elemetsCount = selectedOptions.length;
    for (let i = elemetsCount - 1; i >= 0; i--)
        selectedOptions[i].remove();
}

/**
 * Удалить базовое наименование
 */
function DeleteBaseName() {
    // TODO это временный функционал!

    overlap.show();

    const selectedValue = baseNameInp.value;

    $.ajax({
        type: "POST",
        url: "/Home/DeleteBaseName",
        data: { baseName: selectedValue },
        success: function (res) {
            // Запомнить базовые названия
            FillBaseNames(res.baseNames);
            baseNameInp.value = '';
        },
        error: AjaxErrorLog,
        complete: function () {
            overlap.hide();
        }
    });
}