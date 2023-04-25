const fs = require('fs');
const jimp = require('jimp');
const extract = require('png-chunks-extract');
const encode = require('png-chunks-encode');
const PNGtext = require('png-chunk-text');
const ExifReader = require('exifreader');
const webp = require('webp-converter');
const path = require('path');

async function loadJsonFromPng(img_url, input_format) {
    let format;
    if (input_format === undefined) {
        if (img_url.indexOf('.webp') !== -1) {
            format = 'webp';
        } else {
            format = 'png';
        }
    } else {
        format = input_format;
    }

    switch (format) {
        case 'webp':
            const exif_data = await ExifReader.load(fs.readFileSync(img_url));
            const char_data = exif_data['UserComment']['description'];
            if (char_data === 'Undefined' && exif_data['UserComment'].value && exif_data['UserComment'].value.length === 1) {
                return exif_data['UserComment'].value[0];
            }
            return char_data;
        case 'png':
            const buffer = fs.readFileSync(img_url);
            const chunks = extract(buffer);

            const textChunks = chunks.filter(function (chunk) {
                return chunk.name === 'tEXt';
            }).map(function (chunk) {
                return PNGtext.decode(chunk.data);
            });
            var base64DecodedData = Buffer.from(textChunks[0].text, 'base64').toString('utf8');
            return base64DecodedData;
        default:
            break;
    }
}

module.exports = {
    loadJsonFromPng,
};
