// TESTE DE ESTRESSE: empurra a Api além do uso normal, SEM pausa entre as requisições
// e com uma rampa agressiva de usuários, pra descobrir ONDE ela começa a degradar ou
// quebrar.
//
// Pergunta que ele responde: "qual é o limite da Api, e como ela se comporta quando
// passa dele?"
//
// Atenção: aqui falhar é esperado e faz parte do aprendizado. As metas (thresholds) estão
// frouxas de propósito; o que importa é olhar os números no final.
//
// Rodar:  k6 run loadtests/stress-test.js

import { fazerLogin, criarEConsultarPedido } from './helpers.js';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '30s', target: 100 },
    { duration: '30s', target: 200 },
    { duration: '30s', target: 300 },
    { duration: '30s', target: 0 },
  ],

  thresholds: {
    // Só pra o k6 sinalizar quando a coisa fica realmente feia.
    http_req_failed: ['rate<0.10'],    // até 10% de falhas
    http_req_duration: ['p(95)<2000'], // 95% abaixo de 2 segundos
  },
};

export function setup() {
  return { token: fazerLogin() };
}

// Sem sleep(): cada usuário virtual dispara a próxima rodada assim que termina a anterior.
export default function (dados) {
  criarEConsultarPedido(dados.token);
}
