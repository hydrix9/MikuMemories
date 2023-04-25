const fs = require("fs");
const PNG = require("pngjs").PNG;

function loadJsonFromPng(filePath) {
  return new Promise((resolve, reject) => {
    fs.createReadStream(filePath)
      .pipe(new PNG())
      .on("parsed", function () {
        let jsonData = "";
        for (let y = 0; y < this.height; y++) {
          for (let x = 0; x < this.width; x++) {
            let idx = (this.width * y + x) << 2;
            let r = this.data[idx];
            if (r !== 0) {
              jsonData += String.fromCharCode(r);
            }
          }
        }
        resolve(jsonData);
      })
      .on("error", function (error) {
        reject(error);
      });
  });
}

module.exports = {
  loadJsonFromPng: loadJsonFromPng,
};
