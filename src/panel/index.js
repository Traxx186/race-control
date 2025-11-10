const panel = new Panel('flag-panel');
//const socket = new WebSocket('wss://race-control.justinvanderkruit.nl');
const socket = new WebSocket('ws://localhost:5000');
let latency = 0;

socket.addEventListener('message', (message) => {
    const { event, data } = JSON.parse(message.data);

    switch (event.toLowerCase()) {
        case 'sessionchange':
            latency = data.latency * 1000;
            console.log(latency);
            break;
        case 'flagchange':
            console.log(latency);
            setTimeout(() => {
                handleFlagChangeMessage(data.flag)
            }, latency);
            break;
        default:
            console.error(`${event} not supported`);
    }
});

const handleFlagChangeMessage = (flag) => {
    switch (flag) {
        case 'Clear':
            panel.greenFlag();
            break;
        case 'Yellow':
            panel.yellowFlag();
            break;
        case 'DoubleYellow':
            panel.doubleYellowFlag();
            break;
        case 'Blue':
            panel.blueFlag(data.Driver ?? null);
            break;
        case 'BlackWhite':
            panel.blackWhiteFlag(data.Driver);
            break;
        case 'Red':
            panel.redFlag();
            break;
        case 'SafetyCar':
            panel.safetyCar('SC');
            break;
        case 'Fyc':
            panel.fullCourseYellow();
            break;
        case 'Vsc':
            panel.safetyCar('VSC');
            break;
        case 'Chequered':
            panel.chequeredFlag();
            break;
        case 'Surface':
            panel.slipperySurfaceFlag();
            break;
        default:
            console.warn(`Flag ${flag} not supported`);
    }
}
