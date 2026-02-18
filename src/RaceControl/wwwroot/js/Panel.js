class Panel {
    canvas;
    ctx;
    #interval;
    #currentFlag;

    constructor(canvasId) {
        this.canvas = document.getElementById(canvasId);
        this.#resizeCanvas();

        window.addEventListener('resize', this.#resizeCanvas(), false);
    }

    setFlag(flag, driver = null) {
        switch (flag) {
            case 'Clear':
                this.#greenFlag();
                break;
            case 'Yellow':
                this.#yellowFlag();
                break;
            case 'DoubleYellow':
                this.#doubleYellowFlag();
                break;
            case 'Blue':
                this.#blueFlag(driver);
                break;
            case 'BlackWhite':
                this.#blackWhiteFlag(driver);
                break;
            case 'Red':
                this.#redFlag();
                break;
            case 'SafetyCar':
                this.#safetyCar('SC');
                break;
            case 'Fyc':
                this.#fullCourseYellow();
                break;
            case 'Vsc':
                this.#safetyCar('VSC');
                break;
            case 'Chequered':
                this.#chequeredFlag();
                break;
            case 'Surface':
                this.#slipperySurfaceFlag();
                break;
            default:
                console.warn(`Flag ${flag} not supported`);
        }
    }
    
    #resizeCanvas() {
        this.canvas.height = window.innerHeight;
        this.canvas.width = window.innerWidth;
            
        this.ctx = this.canvas.getContext("2d");
    }

    #greenFlag() {
        const { width, height } = this.canvas;

        this.#currentFlag = 'Clear';
        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.fillStyle = '#00ff50';
            this.ctx.fillRect(0, 0, width, height);

            setTimeout(() => {
                this.ctx.reset();
            }, 250);
        }, 500);

        setTimeout(() => {
            if (this.#currentFlag !== "Clear")
                return;

            this.ctx.reset();
            clearInterval(this.#interval);
        }, 30_000);
    }

    #yellowFlag() {
        const { width, height } = this.canvas;

        this.#currentFlag = 'Yellow';
        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.fillStyle = '#fedd00';
            this.ctx.fillRect(0, 0, width, height);

            setTimeout(() => {
                this.ctx.reset();
            }, 250);
        }, 500);
    }

    #redFlag() {
        const { width, height } = this.canvas;

        this.#currentFlag = 'Red';
        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.fillStyle = '#ff0000';
            this.ctx.fillRect(0, 0, width, height);

            setTimeout(() => {
                this.ctx.reset();
            }, 250);
        }, 500);
    }

    #doubleYellowFlag() {
        const { width, height } = this.canvas;

        this.#currentFlag = 'DoubleYellow';
        this.ctx.reset();
        clearInterval(this.#interval)

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(0, 0);
            this.ctx.lineTo(width, height);
            this.ctx.lineTo(0, height);
            this.ctx.fill();

            setTimeout(() => {
                this.ctx.reset();

                this.ctx.beginPath();
                this.ctx.fillStyle = '#fedd00';
                this.ctx.moveTo(0, 0);
                this.ctx.lineTo(width, 0);
                this.ctx.lineTo(width, height);
                this.ctx.fill();
            }, 125);
        }, 250);
    }

    #safetyCar(text) {
        const { width, height } = this.canvas;

        this.#currentFlag = 'SafetyCar';
        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.strokeStyle = '#fedd00';
            this.ctx.lineWidth = 125;
            this.ctx.strokeRect(0, 0, width, height);

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(0, 0);
            this.ctx.lineTo(250, 0);
            this.ctx.lineTo(0, 250);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(width, 0);
            this.ctx.lineTo(width - 250, 0);
            this.ctx.lineTo(width, 250);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(width, height);
            this.ctx.lineTo(width, height - 250);
            this.ctx.lineTo(width - 250, height);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(0, height);
            this.ctx.lineTo(0, height - 250);
            this.ctx.lineTo(250, height);
            this.ctx.fill();

            this.ctx.font = "25em Arial";
            this.ctx.fillStyle = '#fff';
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle'; 
            this.ctx.fillText(text, Math.floor(width / 2), Math.floor(height / 2));

            setTimeout(() => {
                this.ctx.reset();

                this.ctx.font = "25em Arial";
                this.ctx.fillStyle = '#fff';
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'middle'; 
                this.ctx.fillText(text, Math.floor(width / 2), Math.floor(height / 2));
            }, 250);
        }, 500);
    }

    #fullCourseYellow() {
        const { width, height } = this.canvas;

        this.#currentFlag = 'FCY';
        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.strokeStyle = '#fedd00';
            this.ctx.lineWidth = 125;
            this.ctx.strokeRect(0, 0, width, height);

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(0, 0);
            this.ctx.lineTo(250, 0);
            this.ctx.lineTo(0, 250);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(width, 0);
            this.ctx.lineTo(width - 250, 0);
            this.ctx.lineTo(width, 250);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(width, height);
            this.ctx.lineTo(width, height - 250);
            this.ctx.lineTo(width - 250, height);
            this.ctx.fill();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fedd00';
            this.ctx.moveTo(0, height);
            this.ctx.lineTo(0, height - 250);
            this.ctx.lineTo(250, height);
            this.ctx.fill();

            this.ctx.font = "25em Arial";
            this.ctx.fillStyle = '#fff';
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle'; 
            this.ctx.fillText('FYC', Math.floor(width / 2), Math.floor(height / 2));

            setTimeout(() => {
                this.ctx.reset();

                this.ctx.fillStyle = '#fedd00';
                this.ctx.fillRect(0, 0, width, height);    
            }, 250);
        }, 500);
    }

    #blueFlag(number = null) {
        const { width, height } = this.canvas;

        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            if (number) {
                this.ctx.strokeStyle = '#0a16db';
                this.ctx.lineWidth = 125;
                this.ctx.strokeRect(0, 0, width, height);
    
                this.ctx.font = "25em Arial";
                this.ctx.fillStyle = '#fff';
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'middle'; 
                this.ctx.fillText(number, Math.floor(width / 2), Math.floor(height / 2));
            } else {
                this.ctx.fillStyle = '#0a16db';
                this.ctx.fillRect(0, 0, width, height);
            }

            setTimeout(() => {
                this.ctx.reset();

                if (number) {
                    this.ctx.fillStyle = '#0a16db';
                    this.ctx.fillRect(0, 0, width, height);
                }   
            }, 250);
        }, 500);

        setTimeout(() => {
            if (this.#currentFlag !== "Clear")
                return;

            this.ctx.reset();
            clearInterval(this.#interval);
        }, 10_000);
    }

    #blackWhiteFlag(number) {
        const { width, height } = this.canvas;

        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();

            this.ctx.beginPath();
            this.ctx.fillStyle = '#fff';
            this.ctx.moveTo(0, 0);
            this.ctx.lineTo(width, height);
            this.ctx.lineTo(0, height);
            this.ctx.fill();

            setTimeout(() => {
                this.ctx.reset();

                this.ctx.font = "25em Arial";
                this.ctx.fillStyle = '#fff';
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'middle'; 
                this.ctx.fillText(number, Math.floor(width / 2), Math.floor(height / 2));
            }, 500);
        }, 1000);

        setTimeout(() => {
            if (this.#currentFlag !== "Clear")
                return;

            this.ctx.reset();
            clearInterval(this.#interval);
        }, 10_000);
    }

    #chequeredFlag() {
        const { width, height } = this.canvas;
        const squareSize = 250;
        const rows = Math.ceil(height / squareSize);
        const cols = Math.ceil(width / squareSize);

        this.ctx.reset();
        clearInterval(this.#interval);

        this.#interval = setInterval(() => {
            this.ctx.reset();
    
            for (let i = 0; i < rows; i++) {
                for (let j = 0; j < cols; j++) {
                    this.ctx.fillStyle = (i + j) % 2 === 0 ? '#000' : '#fff';
                    this.ctx.fillRect(j * squareSize, i * squareSize, squareSize, squareSize);
                }
            }

            setTimeout(() => {
                this.ctx.reset();
        
                for (let i = 0; i < rows; i++) {
                    for (let j = 0; j < cols; j++) {
                        this.ctx.fillStyle = (i + j) % 2 === 0 ? '#fff' : '#000';
                        this.ctx.fillRect(j * squareSize, i * squareSize, squareSize, squareSize);
                    }
                }
            }, 250);
        }, 500);

        setTimeout(() => {
            this.ctx.reset();
            clearInterval(this.#interval);
        }, 30_000);
    }

    #slipperySurfaceFlag() {
        const { width, height } = this.canvas;
        const stripeWidth = width / 4;
        const spacing = stripeWidth * 0.128;

        this.ctx.reset();
        clearInterval(this.#interval);

        this.ctx.fillStyle = '#ff0000';
        this.ctx.fillRect(0, 0, stripeWidth - spacing, height);

        this.ctx.fillStyle = '#fedd00';
        this.ctx.fillRect(stripeWidth, 0, stripeWidth - spacing, height);

        this.ctx.fillStyle = '#ff0000';
        this.ctx.fillRect(stripeWidth * 2, 0, stripeWidth - spacing, height);

        this.ctx.fillStyle = '#fedd00';
        this.ctx.fillRect(stripeWidth * 3, 0, stripeWidth - spacing, height);

        setTimeout(() => {
            if (this.#currentFlag !== "Clear")
                return;

            this.ctx.reset();
            clearInterval(this.#interval);
        }, 10_000);
    }
}