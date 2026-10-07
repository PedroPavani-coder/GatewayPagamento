// TESTE DE CARGA: simula um uso "realista e intenso" da Api, com usuários virtuais
// subindo aos poucos, ficando um tempo no pico, e depois descendo.
//
// Pergunta que ele responde: "a Api se comporta bem sob o movimento esperado?"
//
// Rodar:  k6 run loadtests/load-test.js

import { sleep } from 'k6';
import { fazerLogin, criarEConsultarPedido } from './helpers.js';

export const options = {
  // "stages" = etapas. VU = Virtual User (usuário virtual).
  stages: [
    { duration: '30s', target: 20 }, // sobe de 0 até 20 usuários em 30s
    { duration: '1m', target: 20 },  // segura 20 usuários por 1 minuto
    { duration: '30s', target: 50 }, // sobe até 50 usuários
    { duration: '1m', target: 50 },  // segura 50 usuários por 1 minuto
    { duration: '30s', target: 0 },  // desce até 0
  ],

  // "thresholds" = metas. Se alguma não for cumprida, o k6 termina com status de falha
  // (e dá pra usar isso até no CI, no futuro).
  thresholds: {
    http_req_failed: ['rate<0.01'],   // menos de 1% das requisições podem falhar
    http_req_duration: ['p(95)<500'], // 95% das requisições abaixo de 500 ms
    erros_de_fluxo: ['rate<0.01'],    // menos de 1% de falhas no fluxo criar+consultar
  },
};

// Roda UMA vez, antes de tudo. O que ela devolver chega como argumento em default().
export function setup() {
  return { token: fazerLogin() };
}

// Roda em loop, em paralelo, para cada usuário virtual.
export default function (dados) {
  criarEConsultarPedido(dados.token);

  // Pausa de 1s entre rodadas: um usuário de verdade não dispara requisições sem parar.
  sleep(1);
}
